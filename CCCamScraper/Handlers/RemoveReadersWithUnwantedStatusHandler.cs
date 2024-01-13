using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.Handlers
{
    public class RemoveReadersWithUnwantedStatusHandler : IHandler
    {
        private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;
        private IHandler _nextHandler;

        public RemoveReadersWithUnwantedStatusHandler(IOptionsMonitor<CCCamScraperOptions> ccCamScraperOptions)
        {
            _cccamScraperOptions = ccCamScraperOptions;
        }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return _nextHandler;
        }

        public async Task<object> Handle(IJobExecutionContext context)
        {
            var osCamLinesFromStatusPage = await ScraperJobOperations
                .GetListWithCurrentServerStatusFromOsCamStatusPage(_cccamScraperOptions.CurrentValue.OsCamStatusPageUrl)
                .ConfigureAwait(false);

            if (osCamLinesFromStatusPage.Count == 0)
            {
                Log.Error("No readers retrieved from the OSCAM status page, OsCam server restart missing maybe?");
                return new List<OsCamReader>();
            }

            context.Result = await RemoveReadersThatHaveUnwantedStatus(
                ((List<OsCamReader>)context.Result!),
                osCamLinesFromStatusPage,
                _cccamScraperOptions.CurrentValue)
                .ConfigureAwait(false);

            return _nextHandler.Handle(context);
        }

        private Task<List<OsCamReader>> RemoveReadersThatHaveUnwantedStatus(
            List<OsCamReader> currentListOfCcCamReadersFromFile,
            List<OscamUiStatusLine> currentServerStatusList,
            CCCamScraperOptions scraperOptions)
        {
            var readersToRemove = new List<OsCamReader>();

            foreach (var osCamuiReader in currentServerStatusList)
            {
                if (scraperOptions.UnwantedStatus.Contains(osCamuiReader.Status))
                {
                    var reader =
                        currentListOfCcCamReadersFromFile.Where(
                            camReader => camReader.Label == osCamuiReader.ReaderUser);

                    readersToRemove.AddRange(reader);

                    Log.Information(osCamuiReader.ReaderUser + " with status " + osCamuiReader.Status +
                                    " is flagged to be deleted.");
                }
            }

            if (readersToRemove.Count > 0)
                currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList();

            return Task.FromResult(currentListOfCcCamReadersFromFile);
        }
    }
}