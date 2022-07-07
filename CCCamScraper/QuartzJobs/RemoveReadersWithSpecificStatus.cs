using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCCamScraper.QuartzJobs
{
    [DisallowConcurrentExecution]
    public class RemoveReadersWithUnwantedStatus : IJob
    {
        private readonly CCCamScraperOptions cccamScraperOptions;
        private readonly CCCamScraperJobOption? quartzJobsOption;

        public RemoveReadersWithUnwantedStatus(IServiceProvider serviceProvider)
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
                Log.Error("Couldn't find Scraper options");
                throw new ArgumentNullException("Couldn't find Scraper options");
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await CheckCCCamServerstate().ConfigureAwait(false);
        }

        public async Task CheckCCCamServerstate()
        {
            try
            {
                Log.Information(
                    $"Started removing readers from oscam.server file with the following status: {string.Join(", ", cccamScraperOptions.UnwantedStatus)}");

                var readersFromOscamServer = await ScraperJobOperations
                    .GetListWithCurrentReadersOnOscamServerFile(cccamScraperOptions.OscamServerPath)
                    .ConfigureAwait(false);

                var oscamLinesFromStatusPage = await ScraperJobOperations
                    .GetListWithCurrentServerStatusFromOsCam(cccamScraperOptions.OsCamStatusPageURL)
                    .ConfigureAwait(false);

                if (oscamLinesFromStatusPage.Count == 0)
                {
                    Log.Error("No readers retrieved from the OSCAM status page, OsCam server restart missing maybe?");
                    return;
                }

                readersFromOscamServer =
                    await RemoveReadersThatHaveUnwantedStatus(readersFromOscamServer, oscamLinesFromStatusPage,
                        cccamScraperOptions).ConfigureAwait(false);

                ScraperJobOperations.WriteOsCamReadersToFile(readersFromOscamServer,
                    cccamScraperOptions
                        .OscamServerPath); // + DateTime.Now.ToShortTimeString().Replace(":","") + ".txt");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public Task<List<OsCamReader>> RemoveReadersThatHaveUnwantedStatus(
            List<OsCamReader> currentListOfCcCamReadersFromFile, List<OscamUiStatusLine> currentServerStatusList,
            CCCamScraperOptions scraperOptions)
        {
            var readersToRemove = new List<OsCamReader>();

            foreach (var osCAMUIReader in currentServerStatusList)
                if (scraperOptions.UnwantedStatus.Contains(osCAMUIReader.Status))
                {
                    var reader = currentListOfCcCamReadersFromFile.Where(camReader => camReader.Label == osCAMUIReader.ReaderUser);

                    readersToRemove.AddRange(reader);

                    Log.Information(osCAMUIReader.ReaderUser + " with status " + osCAMUIReader.Status +
                                    " is flagged to be deleted.");
                }

            if (readersToRemove.Count > 0)
                currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList();

            return Task.FromResult(currentListOfCcCamReadersFromFile);
        }
    }
}