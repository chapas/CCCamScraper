using CCCamScraper.Configurations;
using CCCamScraper.Handlers;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.QuartzJobs.Jobs;

[DisallowConcurrentExecution]
public class RestartOsCamJob : IJob
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;

    public RestartOsCamJob(IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
    {
        _cccamScraperOptions = cccamScraperOptions;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Information($"Started executing OSCam Restart Job: {context.JobDetail.Key.Name}");

        IHandler handler = new RestartOsCamJobHandler(_cccamScraperOptions);

        await handler.Handle(context).ConfigureAwait(false);

        Log.Information("RestartOsCamJob execution chain finished.");
    }
}