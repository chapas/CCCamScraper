namespace CCCamScraper.Handlers;

using CCCamScraper.Configurations;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Net.Http;
using System.Threading;

public class RestartOsCamJobHandler : IHandler
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;
    private IHandler? _nextHandler;

    private static readonly SemaphoreSlim _restartLock = new SemaphoreSlim(1, 1);

    public RestartOsCamJobHandler(IOptionsMonitor<CCCamScraperOptions> ccCamScraperOptions)
    {
        _cccamScraperOptions = ccCamScraperOptions;
    }

    public IHandler SetNext(IHandler handler)
    {
        _nextHandler = handler;
        return _nextHandler;
    }

    public async Task<object> Handle(IJobExecutionContext context)
    {
        string statusPageUrl = _cccamScraperOptions.CurrentValue.OsCamStatusPageUrl;
        var uri = new Uri(statusPageUrl);
        string baseUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        string restartUrl = $"{baseUrl}/shutdown.html?action=Restart";

        Log.Information("Waiting for access to OSCam restart controller...");
        await _restartLock.WaitAsync().ConfigureAwait(false);

        try
        {
            bool isAlive = await CheckOscamAlive(statusPageUrl).ConfigureAwait(false);

            if (!isAlive)
            {
                Log.Warning("OSCam is already unresponsive. It is likely already restarting from a previous job. Skipping redundant restart.");
                await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var request = new HttpRequestMessage(HttpMethod.Get, restartUrl);
                    request.Headers.Add("Referer", $"{baseUrl}/shutdown.html");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                    Log.Information("Sending Restart command to OSCam...");
                    var response = await client.SendAsync(request).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("OSCam restart command accepted.");
                        Log.Information("Waiting 10 seconds for OSCam service to initialize...");
                        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                        Log.Information("Wait complete.");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Log.Warning("OSCam connection reset during restart request. This usually means the restart was triggered successfully.");
                    await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in RestartOsCamJobHandler.");
        }
        finally
        {
            _restartLock.Release();
        }

        if (_nextHandler != null)
        {
            return await _nextHandler.Handle(context).ConfigureAwait(false);
        }

        return context.Result ?? new object();
    }

    private async Task<bool> CheckOscamAlive(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync(url).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}