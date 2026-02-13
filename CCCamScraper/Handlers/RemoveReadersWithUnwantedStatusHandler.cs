using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.Handlers;

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

        if (!osCamLinesFromStatusPage.Any())
        {
            Log.Error("No readers retrieved from the OSCAM status page, OsCam server restart missing maybe?");
            return new List<OsCamReader>();
        }

        context.Result = await RemoveReadersThatHaveUnwantedStatus(
                ((List<OsCamReader>)context.Result!),
                osCamLinesFromStatusPage,
                _cccamScraperOptions.CurrentValue)
            .ConfigureAwait(false);

        if (_nextHandler != null)
        {
            return await _nextHandler.Handle(context).ConfigureAwait(false);
        }

        return context.Result;
    }

    private Task<List<OsCamReader>> RemoveReadersThatHaveUnwantedStatus(
        List<OsCamReader> currentListOfCcCamReadersFromFile,
        List<OscamUiStatusLine> currentServerStatusList,
        CCCamScraperOptions scraperOptions)
    {
        var unwantedReadersFromUI = currentServerStatusList
            .Where(line => scraperOptions.UnwantedStatus.Contains(line.Status))
            .ToList();

        Log.Information("Found {count} readers with unwanted status", unwantedReadersFromUI.Count);

        var readersToRemove = new List<OsCamReader>();

        foreach (var uiReader in unwantedReadersFromUI)
        {
            foreach (var reader in currentListOfCcCamReadersFromFile.Where(reader => reader.Label.StartsWith(uiReader.ReaderUser)))
            {
                readersToRemove.Add(reader);
                Log.Information($"{reader.Label} with status {uiReader.Status} is flagged to be deleted.");
            }
        }

        currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove, new OsCamReaderComparer()).ToList();

        return Task.FromResult(currentListOfCcCamReadersFromFile);
    }
}