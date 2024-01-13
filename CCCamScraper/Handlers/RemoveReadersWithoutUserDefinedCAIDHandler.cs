using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using System.Xml.Serialization;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.Handlers
{
    public class RemoveReadersWithoutUserDefinedCAIDHandler : IHandler
    {
        private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;
        private IHandler _nextHandler;

        public RemoveReadersWithoutUserDefinedCAIDHandler(IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
        {
            _cccamScraperOptions = cccamScraperOptions;
        }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return _nextHandler;
        }

        public async Task<object> Handle(IJobExecutionContext context)
        {
            var oscamLinesFromStatusPage = await ScraperJobOperations
                .GetListWithCurrentServerStatusFromOsCamStatusPage(_cccamScraperOptions.CurrentValue.OsCamStatusPageUrl)
                .ConfigureAwait(false);

            context.Result = await RemoveReadersThatDontHaveTheCaid(
                (List<OsCamReader>)context.Result,
                oscamLinesFromStatusPage,
                _cccamScraperOptions.CurrentValue);

            return _nextHandler.Handle(context);
        }

        public async Task<List<OsCamReader>> RemoveReadersThatDontHaveTheCaid(
            List<OsCamReader> currentListOfCcCamReadersFromFile,
            List<OscamUiStatusLine> currentServerStatusList,
            CCCamScraperOptions scraperOptions)
        {
            var readersToRemove = new List<OsCamReader>();

            foreach (var osCamReader in currentListOfCcCamReadersFromFile)
            {
                var readerHasCaidFromUserAllowedCaids =
                    await HasReaderAUserDefinedCaid(
                        $"{scraperOptions.OsCamReaderApiurl}?part=entitlement&label={osCamReader.Label}",
                        scraperOptions.CaiDs).ConfigureAwait(false);

                if (readerHasCaidFromUserAllowedCaids || scraperOptions.ExcludedFromDeletion.Contains(osCamReader.Label))
                    continue;

                readersToRemove.Add(osCamReader);
                Log.Information($"{osCamReader.Label} does not have a valid CAID and is flagged to be deleted");
            }

            return readersToRemove.Count > 0
                ? currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList()
                : currentListOfCcCamReadersFromFile;
        }

        private async Task<bool> HasReaderAUserDefinedCaid(string osCamReaderPageUrl, string[] caiDs)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(osCamReaderPageUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");

                var response = await httpClient.GetAsync(osCamReaderPageUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Didn't had access to the oscam reader details page: {osCamReaderPageUrl}");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var serializer = new XmlSerializer(typeof(oscam));
                using var reader = new StringReader(content);
                var oscamData = (oscam)serializer.Deserialize(reader);

                var totalCardCount = oscamData.reader?.FirstOrDefault()?.cardlist.FirstOrDefault()?.totalcards;

                if (totalCardCount == null || int.Parse(totalCardCount) == 0)
                    return false;

                if (!caiDs.Any())
                    return true;

                var hasCaid = oscamData.reader.FirstOrDefault()?.cardlist.FirstOrDefault()?.card
                    .Any(card => caiDs.Any(caid => card.caid.Contains(caid)));

                return hasCaid ?? false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Didn't had access to the oscam reader details page: {osCamReaderPageUrl}");
                return true; // this is a bit special, if it throws we don't care and continue to the next line
            }
        }
    }
}