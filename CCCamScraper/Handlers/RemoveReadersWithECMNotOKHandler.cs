using CCCamScraper.Models;
using CCCamScraper.Configurations;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace CCCamScraper.Handlers;

public class RemoveReadersWithECMNotOKHandler : IHandler
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _options;
    private IHandler _nextHandler;

    public RemoveReadersWithECMNotOKHandler(IOptionsMonitor<CCCamScraperOptions> options)
    {
        _options = options;
    }

    public IHandler SetNext(IHandler handler)
    {
        _nextHandler = handler;
        return _nextHandler;
    }

    public async Task<object> Handle(IJobExecutionContext context)
    {
        var osCamUIReaderStatusLines = await ScraperJobOperations
            .GetListWithCurrentReadersStatusFromOsCamPage(_options.CurrentValue.OsCamReadersPageUrl)
            .ConfigureAwait(false);

        if (!osCamUIReaderStatusLines.Any())
        {
            Log.Error("No readers retrieved from the OSCAM readers page. Check connection or URL: {Url}",
                _options.CurrentValue.OsCamReadersPageUrl);

            return new List<OsCamReader>();
        }
        
        context.Result = await RemoveReadersWithUnwantedStatus(
                ((List<OsCamReader>)context.Result!),
                osCamUIReaderStatusLines)
            .ConfigureAwait(false);

        if (_nextHandler != null)
        {
            return await _nextHandler.Handle(context).ConfigureAwait(false);
        }

        return context.Result ?? new object();
    }

    /// <summary>
    /// Removes readers from the current list if they match the "bad performance" criteria from the UI.
    /// </summary>
    private async Task<List<OsCamReader>> RemoveReadersWithUnwantedStatus(
        List<OsCamReader> currentReaders,
        List<OscamUIReaderStatus> uiStatuses)
    {
        if (currentReaders == null || !currentReaders.Any())
        {
            return new List<OsCamReader>()!;
        }

        var okThreshold = _options.CurrentValue.EcmOkThreshold;
        var nokThreshold = _options.CurrentValue.EcmNokThreshold;

        var readersToRemove = new List<OsCamReader>();

        var badUiReaders = uiStatuses.Where(ui => ui.OK == okThreshold && ui.NOK > nokThreshold).ToList();

        foreach (var uiReader in badUiReaders)
        {
            var matches = currentReaders
                .Where(reader => reader.Label.StartsWith(uiReader.Reader, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var reader in matches)
            {
                readersToRemove.Add(reader);
                Log.Information("{Label} with ECM stats (OK: {OK}, NOK: {NOK}) is flagged to be deleted.",
                    reader.Label, uiReader.OK, uiReader.NOK);
            }
        }

        if (readersToRemove.Any())
        {
            foreach (var readerToDelete in readersToRemove.Distinct())
            {
                currentReaders.Remove(readerToDelete);
            }

            Log.Information("Maintenance Cleanup: Total of {Count} readers removed from the list.", readersToRemove.Count);
        }
        else
        {
            Log.Information("Maintenance Cleanup: No readers met the deletion criteria OKECM = {okThreshold} and NOKECM > {nokThreshold}", okThreshold, nokThreshold);
        }

        return currentReaders;
    }
}