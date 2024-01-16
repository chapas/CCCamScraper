using Quartz;
using Serilog;
using CCCamScraper.Configurations;
using CCCamScraper.Handlers;
using Microsoft.Extensions.Options;

namespace CCCamScraper.QuartzJobs.Jobs;

[DisallowConcurrentExecution]
public class RemoveReadersWithUnwantedStatusJob : IJob
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;

    public RemoveReadersWithUnwantedStatusJob(
        IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
    {
        _cccamScraperOptions = cccamScraperOptions;
    }

    public async Task Execute(IJobExecutionContext context)
    { 
        Log.Information($"Started removing readers from oscam.server file with the following status: {string.Join(", ", _cccamScraperOptions.CurrentValue.UnwantedStatus)}");


        IHandler handler = new GetCurrentReadersOnOscamServerFileHandler(_cccamScraperOptions);
        handler
            .SetNext(new RemoveReadersWithUnwantedStatusHandler(_cccamScraperOptions))
            .SetNext(new WriteOsCamReadersToFileHandler(_cccamScraperOptions));

        await handler.Handle(context);
    }
}