using CCCamScraper.Configurations;
using CCCamScraper.Handlers;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.QuartzJobs.Jobs;

public class ScrapeJob : IJob
{
    private readonly IOptionsMonitor<QuartzJobsOptions> _quartzJobsOptions;
    private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;

    public ScrapeJob(
        IOptionsMonitor<QuartzJobsOptions> quartzJobsOptions,
        IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
    {
        _quartzJobsOptions = quartzJobsOptions;
        _cccamScraperOptions = cccamScraperOptions;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Information($"Started executing a scrape job named: {context.JobDetail.Key.Name}");

        var quartzJobOption = _quartzJobsOptions.CurrentValue.CcCamScraperJobs.FirstOrDefault(qjob => qjob.Name == context.JobDetail.Key.Name)
                              ?? throw new InvalidOperationException();

        IHandler handler = new GetCurrentReadersOnOscamServerFileHandler(_cccamScraperOptions);
        handler
            .SetNext(new ScrapeCLinesFromUrlHandler(quartzJobOption.UrlToScrape, _cccamScraperOptions))
            .SetNext(new WriteOsCamReadersToFileHandler(_cccamScraperOptions));

        await handler.Handle(context);
    }
}