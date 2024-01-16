using Quartz;
using Serilog;
using CCCamScraper.Configurations;
using CCCamScraper.Handlers;
using Microsoft.Extensions.Options;

namespace CCCamScraper.QuartzJobs.Jobs;

[DisallowConcurrentExecution]
public class RemoveReadersWithoutUserDefinedCaidJob : IJob
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;

    public RemoveReadersWithoutUserDefinedCaidJob(
        IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
    {
        _cccamScraperOptions = cccamScraperOptions;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_cccamScraperOptions.CurrentValue.CaiDs.Length == 0)
        {
            Log.Information("No user CAID's defined, skipping Job");
            return;
        }

        Log.Information("Started removing readers from oscam.server file without users CAID's");

        IHandler handler = new GetCurrentReadersOnOscamServerFileHandler(_cccamScraperOptions);
        handler
            .SetNext(new RemoveReadersWithoutUserDefinedCAIDHandler(_cccamScraperOptions))
            .SetNext(new WriteOsCamReadersToFileHandler(_cccamScraperOptions));

        await handler.Handle(context);
    }
}