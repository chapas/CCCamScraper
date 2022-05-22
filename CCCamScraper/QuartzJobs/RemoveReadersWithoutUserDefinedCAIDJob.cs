using CCCamScraper.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CCCamScraper.QuartzJobs
{
    [DisallowConcurrentExecution]
    public class RemoveReadersWithoutUserDefinedCAIDJob : IJob
    {
        CCCamScraperJobOption? quartzJobsOption;
        CCCamScraperOptions cccamScraperOptions;

        public RemoveReadersWithoutUserDefinedCAIDJob(IServiceProvider serviceProvider)
        {
            quartzJobsOption = serviceProvider.GetRequiredService<QuartzJobsOptions>().CCCamScraperJobs.FirstOrDefault(qjob => qjob.Name == GetType().Name);
            if (quartzJobsOption == null)
            {
                Log.Error($"Couldn't find a Quartz job named: {GetType().Name}");
                throw new ArgumentNullException($"Couldn't find a Quartz job named: {GetType().Name}");
            }

            cccamScraperOptions = serviceProvider.GetRequiredService<CCCamScraperOptions>();
            if (cccamScraperOptions == null)
            {
                Log.Error($"Couldn't find Scraper options");
                throw new ArgumentNullException($"Couldn't find Scraper options");
            }
        }

        public async Task Execute(IJobExecutionContext context) => await CheckCCCamServerstate().ConfigureAwait(false);

        public async Task CheckCCCamServerstate()
        {
            try
            {
                Log.Information($"Started removing readers from oscam.server file without users CAID's");

                var readersFromOscamServer = await ScraperJobOperations.GetListWithCurrentReadersOnOscamServerFile(cccamScraperOptions.OscamServerPath).ConfigureAwait(false);

                var oscamLinesFromStatusPage = await ScraperJobOperations.GetListWithCurrentServerStatusFromOsCam(cccamScraperOptions.OsCamStatusPageURL).ConfigureAwait(false);

                if (oscamLinesFromStatusPage.Count == 0)
                {
                    Log.Error("No readers retrieved from the OSCAM status page");
                    return;
                }

                readersFromOscamServer = await ScraperJobOperations.RemoveReadersThatDontHaveTheCAID(readersFromOscamServer, oscamLinesFromStatusPage, cccamScraperOptions).ConfigureAwait(false);

                ScraperJobOperations.WriteOsCamReadersToFile(readersFromOscamServer, cccamScraperOptions.OscamServerPath); // + DateTime.Now.ToShortTimeString().Replace(":","") + ".txt");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}