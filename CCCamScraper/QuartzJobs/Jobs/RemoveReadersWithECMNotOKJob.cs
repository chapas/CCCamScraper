using Quartz;
using Serilog;
using CCCamScraper.Configurations;
using CCCamScraper.Handlers;
using Microsoft.Extensions.Options;

namespace CCCamScraper.QuartzJobs.Jobs;

[DisallowConcurrentExecution]
public class RemoveReadersWithECMNotOKJob : IJob
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;

    public RemoveReadersWithECMNotOKJob(IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
    {
        _cccamScraperOptions = cccamScraperOptions;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Log.Information("Running ECM Maintenance Job...");

        IHandler handler = new GetCurrentReadersOnOscamServerFileHandler(_cccamScraperOptions);

        handler
            .SetNext(new RemoveReadersWithECMNotOKHandler(_cccamScraperOptions))
            .SetNext(new WriteOsCamReadersToFileHandler(_cccamScraperOptions))
            .SetNext(new RestartOsCamJobHandler(_cccamScraperOptions));

        await handler.Handle(context);
    }
}