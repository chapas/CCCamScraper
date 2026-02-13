using AngleSharp;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace CCCamScraper.Handlers;

public class ScrapeCLinesFromUrlHandler : IHandler
{
    private readonly string urlToScrape;
    private readonly IOptionsMonitor<CCCamScraperOptions> _ccCamScraperOptions;
    private IHandler _nextHandler;
    private static readonly Regex cccamRegExPattern = new Regex(@"[cC]: ([\w.-]+ )(\b\d{1,5}\b )([\w.-]+ )([\w.-]+)( # ?[\w.-]+)?", RegexOptions.Compiled);

    public ScrapeCLinesFromUrlHandler(
        string urlToScrape,
        IOptionsMonitor<CCCamScraperOptions> ccCamScraperOptions)
    {
        this.urlToScrape = urlToScrape;
        _ccCamScraperOptions = ccCamScraperOptions;
    }

    public IHandler SetNext(IHandler handler)
    {
        _nextHandler = handler;
        return _nextHandler;
    }

    public async Task<object> Handle(IJobExecutionContext context)
    {
        var osCamReaders = new HashSet<OsCamReader>((context.Result as List<OsCamReader>)!, new OsCamReaderComparer());
        var scrappedLines = await ScrapeCLinesFromUrl(urlToScrape).ConfigureAwait(false);
        osCamReaders.UnionWith(ParseCLines(scrappedLines, context.JobDetail.Key.Name));
        context.Result = osCamReaders.ToList();

        if (_nextHandler != null)
        {
            return await _nextHandler.Handle(context).ConfigureAwait(false);
        }

        return context.Result ?? new object();
    }

    private async Task<HashSet<string>> ScrapeCLinesFromUrl(string urlToScrape)
    {
        var urlToScrapeFrom = UrlStringReplacement(urlToScrape);
        string htmlContent = string.Empty;
        bool useFlareSolverr = false;

        string flareSolverrProxy = _ccCamScraperOptions.CurrentValue.FlareSolverrUrl;

        Log.Information("Using FlareSolverr at: {Url}", flareSolverrProxy);

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(90);

        try
        {
            var checkResponse = await client.GetAsync(flareSolverrProxy.Replace("/v1", ""));
            if (checkResponse.IsSuccessStatusCode)
            {
                useFlareSolverr = true;
                Log.Information("FlareSolverr is ONLINE at {Url}. Using proxy for {Target}", flareSolverrProxy, urlToScrapeFrom);
            }
        }
        catch
        {
            Log.Warning("FlareSolverr is OFFLINE at {Url}. Falling back to regular request.", flareSolverrProxy);
        }

        try
        {
            if (useFlareSolverr)
            {
                var requestPayload = new { cmd = "request.get", url = urlToScrapeFrom, maxTimeout = 60000 };
                var response = await client.PostAsJsonAsync(flareSolverrProxy, requestPayload);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<FlareSolverrResponse>();
                    htmlContent = json?.Solution?.Response ?? string.Empty;
                }
            }
            else
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");
                htmlContent = await client.GetStringAsync(urlToScrapeFrom);
            }
        }
        catch
        {
            Log.Error("Failed to retrieve content from {Url}", urlToScrapeFrom);
        }

        if (string.IsNullOrEmpty(htmlContent)) return new HashSet<string>();

        return await ParseRawHtml(htmlContent);
    }

    // Helper classes for FlareSolverr JSON response
    public record FlareSolverrResponse(string Status, string Message, FlareSolverrSolution Solution);
    public record FlareSolverrSolution(string Response, int Status, string Url);

    private async Task<HashSet<string>> ParseRawHtml(string html)
    {
        try
        {
            var context = BrowsingContext.New(AngleSharp.Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html));
            var sourceText = document.Body?.InnerHtml ?? string.Empty;

            if (sourceText.Contains("checking your browser", StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("Direct request failed: Blocked by Cloudflare/DDOS-Guard. FlareSolverr is required for this site.");
                return new HashSet<string>();
            }

            var lines = sourceText.Split(new[] { "\r\n", "\r", "\n", "<br>", "<br />" }, StringSplitOptions.TrimEntries);
            var cStrings = new HashSet<string>();

            foreach (var line in lines)
            {
                if (line.Contains("c: ", StringComparison.OrdinalIgnoreCase))
                {
                    var matches = cccamRegExPattern.Matches(line);
                    foreach (Match match in matches) if (match.Success) cStrings.Add(match.Value);
                }
            }
            return SplitLinesIntoHashSet(cStrings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Critical parsing error in ParseRawHtml.");
            return new HashSet<string>();
        }
    }

    private static string UrlStringReplacement(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Log.Warning("UrlStringReplacement received a null URL. Skipping replacement.");
            return string.Empty;
        }

        var day = DateTime.Today.Day.ToString("00", CultureInfo.InvariantCulture);
        var month = DateTime.Today.Month.ToString("00", CultureInfo.InvariantCulture);
        var year = DateTime.Today.Year.ToString("0000", CultureInfo.InvariantCulture);

        // Perform case-insensitive replacements
        url = url.Replace("<yyyy>", year, StringComparison.OrdinalIgnoreCase);
        url = url.Replace("<mm>", month, StringComparison.OrdinalIgnoreCase);
        url = url.Replace("<dd>", day, StringComparison.OrdinalIgnoreCase);

        return url;
    }

    private HashSet<OsCamReader> ParseCLines(HashSet<string> cLines, string url)
    {
        var cccamLines = cLines.Select(ParseCLine)
            .Where(cl => cl != null)
            .ToList();

        var readers = cccamLines.Select(cl => new OsCamReader
        {
            Device = cl.Hostname,
            Port = cl.Port,
            User = cl.Username,
            Password = cl.Password,
            Label = cl.Hostname,
            Cccversion = cl.Cccversion,
            Caid = string.Join(",", _ccCamScraperOptions.CurrentValue.CaiDs),
            Description = new OsCamReaderDescription
            {
                AccumulatedError = 0,
                AccumulatedOff = 0,
                AccumulatedUnknown = 0,
                LbValueReader = 0,
                Username = cl.Username,
                ECMOK = 0,
                ECMNOK = 0,
                ECMTOUT = 0
            },
        }).ToHashSet();

        Log.Information($"Parsed {readers.Count} C lines from a total of {cLines.Count} found on {url}");

        return readers;
    }

    private static CcCamLine ParseCLine(string cline)
    {
        Match match = cccamRegExPattern.Match(cline);
        if (!match.Success)
        {
            return null;
        }

        CcCamLine result = new CcCamLine
        {
            Hostname = match.Groups[1].Value.Trim(),
            Port = match.Groups[2].Value.Trim(),
            Username = match.Groups[3].Value.Trim(),
            Password = match.Groups[4].Value.Trim()
        };

        string versionGroup = match.Groups[5].Value.Trim();
        string[] validVersions = { "2.0.11", "2.1.1", "2.1.2", "2.1.3", "2.1.4", "2.2.8", "2.2.1", "2.3.6", "2.3.1", "2.3.2" };
        result.Cccversion = validVersions.FirstOrDefault(version => versionGroup.Contains(version));

        return result;
    }

    public HashSet<string> SplitLinesIntoHashSet(HashSet<string> inputSet)
    {
        if (inputSet.Count > 1)
            return inputSet;

        var input = inputSet.First();

        if (input.Count(sub => sub == 'C') > 1)
        {
            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return new HashSet<string>(lines.Where(line => line.StartsWith("C: ", StringComparison.OrdinalIgnoreCase)));
        }

        return new HashSet<string> { input };
    }
}

public class OsCamReaderComparer : IEqualityComparer<OsCamReader>
{
    public bool Equals(OsCamReader x, OsCamReader y)
    {
        return string.Equals(x.Device, y.Device, StringComparison.InvariantCultureIgnoreCase)
               && string.Equals(x.User, y.User, StringComparison.InvariantCultureIgnoreCase)
               && string.Equals(x.Password, y.Password, StringComparison.InvariantCultureIgnoreCase);
    }

    public int GetHashCode(OsCamReader obj)
    {
        return obj?.Label?.GetHashCode() ?? 0;
    }
}