using System.Xml.Serialization;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace CCCamScraper.QuartzJobs
{
    [DisallowConcurrentExecution]
    public class RemoveReadersWithoutUserDefinedCAIDJob : IJob
    {
        private readonly CCCamScraperOptions cccamScraperOptions;
        private readonly CCCamScraperJobOption? quartzJobsOption;

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
                Log.Information("Started removing readers from oscam.server file without users CAID's");

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
                    await RemoveReadersThatDontHaveTheCAID(readersFromOscamServer, oscamLinesFromStatusPage,
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

        public async Task<List<OsCamReader>> RemoveReadersThatDontHaveTheCAID(List<OsCamReader> currentListOfCcCamReadersFromFile, List<OscamUiStatusLine> currentServerStatusList, CCCamScraperOptions scraperOptions)
        {
            var readersToRemove = new List<OsCamReader>();

            foreach (var osCAMReader in currentListOfCcCamReadersFromFile)
            {
                var readerHasCaidFromUserAllowedCaids =
                    await HasTheReaderAUserDefinedCaid(
                        scraperOptions.OsCamReaderAPIURL + @"?part=entitlement&label=" + osCAMReader.Label,
                        scraperOptions.CAIDs).ConfigureAwait(false);
                ///Let's look for the CAID and if it's there we don't do anything

                if (readerHasCaidFromUserAllowedCaids)
                    continue;

                if (scraperOptions.ExcludedFromDeletion.Contains(osCAMReader.Label))
                    continue;

                readersToRemove.Add(osCAMReader);
                Log.Information(osCAMReader.Label + " does not have a valid CAID and is flagged to be deleted");
            }

            if (readersToRemove.Count > 0)
                currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList();

            return currentListOfCcCamReadersFromFile;
        }

        private async Task<bool> HasTheReaderAUserDefinedCaid(string osCamReaderPageUrl, string[] caiDs)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(oscam));
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(osCamReaderPageUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");

                    var response = await httpClient.GetAsync(osCamReaderPageUrl).ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                        using (var reader = new StringReader(await response.Content.ReadAsStringAsync().ConfigureAwait(false)))
                        {
                            var test = (oscam)serializer.Deserialize(reader);

                            var totalCardCount = test.reader?.Select(oscamReader => oscamReader)
                                .FirstOrDefault()
                                ?.cardlist.FirstOrDefault()
                                ?.totalcards;

                            if (totalCardCount == null || int.Parse(totalCardCount) == 0)
                                return false;

                            if (caiDs.Any())
                                foreach (var caid in caiDs)
                                {
                                    var hasCaid = (test.reader.Select(oscamReader => oscamReader)
                                            .FirstOrDefault()?
                                            .cardlist.FirstOrDefault()?
                                            .card)
                                        .FirstOrDefault(card => card.caid.Contains(caid));

                                    if (hasCaid != null)
                                        return true;
                                }
                            else
                                return true;

                            return false;
                        }

                    Log.Error($"Didn't had access to the oscam reader details page: {osCamReaderPageUrl}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Didn't had access to the oscam reader details page: {osCamReaderPageUrl}");
                return true; // this is a bit special, if it throws we don't care and continue to the next line
            }

            return false;
        }
    }
}