using CCCamScraper.Configurations;
using CCCamScraper.Handlers;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.QuartzJobs.Jobs;

[DisallowConcurrentExecution]
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
        var quartzJobOption = _quartzJobsOptions.CurrentValue.CcCamScraperJobs
                                .FirstOrDefault(qjob => qjob.Name == context.JobDetail.Key.Name)
                              ?? throw new InvalidOperationException();

        if (!quartzJobOption.Enabled)
        {
            Log.Warning("Job {JobName} is currently disabled in settings. Skipping execution.",
                context.JobDetail.Key.Name);
            return;
        }

        bool isStartupTrigger = context.Trigger.Key.Name.Contains("startup", StringComparison.OrdinalIgnoreCase);

        Log.Information("Started executing scrape job: {JobName} (Startup Run: {IsStartup})",
            context.JobDetail.Key.Name, isStartupTrigger);

        if (quartzJobOption.RandomnessInMinutes.HasValue && quartzJobOption.RandomnessInMinutes.Value > 0 && !isStartupTrigger)
        {
            var random = new Random();
            int delayMinutes = random.Next(0, quartzJobOption.RandomnessInMinutes.Value + 1);

            if (delayMinutes > 0)
            {
                Log.Information("Randomness Jitter for {JobName}: Waiting {Min} minutes before starting logic...",
                    quartzJobOption.Name, delayMinutes);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), context.CancellationToken);
            }
        }
        else if (isStartupTrigger)
        {
            Log.Information("Bypassing Jitter for {JobName} to satisfy RunOnceAtStartUp requirement.", quartzJobOption.Name);
        }

        IHandler handler = new GetCurrentReadersOnOscamServerFileHandler(_cccamScraperOptions);
        handler
            .SetNext(new ScrapeCLinesFromUrlHandler(quartzJobOption.UrlToScrape, _cccamScraperOptions))
            .SetNext(new WriteOsCamReadersToFileHandler(_cccamScraperOptions))
            .SetNext(new RestartOsCamJobHandler(_cccamScraperOptions));

        await handler.Handle(context);

        Log.Information("Finished execution chain for {JobName}.", context.JobDetail.Key.Name);
    }
}