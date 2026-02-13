using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Text;

namespace CCCamScraper.Handlers;

public class WriteOsCamReadersToFileHandler : IHandler
{
    private readonly IOptionsMonitor<CCCamScraperOptions> _ccCamScraperOptions;
    private IHandler _nextHandler;

    private static readonly SemaphoreSlim FileLock = new SemaphoreSlim(1, 1);

    public WriteOsCamReadersToFileHandler(IOptionsMonitor<CCCamScraperOptions> ccCamScraperOptions)
    {
        _ccCamScraperOptions = ccCamScraperOptions;
    }

    public IHandler SetNext(IHandler handler)
    {
        _nextHandler = handler;
        return _nextHandler;
    }

    public async Task<object> Handle(IJobExecutionContext context)
    {
        var readersToWrite = context.Result as List<OsCamReader>;

        if (readersToWrite != null && readersToWrite.Any())
        {
            await WriteToFileInternal(readersToWrite, _ccCamScraperOptions.CurrentValue);
        }

        if (_nextHandler != null)
        {
            return await _nextHandler.Handle(context).ConfigureAwait(false);
        }

        return context.Result ?? new object();
    }

    private async Task WriteToFileInternal(List<OsCamReader> currentServerStatusList, CCCamScraperOptions options)
    {
        var uniqueReaders = new List<OsCamReader>();
        foreach (var reader in currentServerStatusList)
        {
            var label = uniqueReaders.Any(r => r.Label == reader.Label)
                ? $"{reader.Label}_({reader.User})"
                : reader.Label;

            reader.Label = label;
            uniqueReaders.Add(reader);
        }

        await FileLock.WaitAsync();

        try
        {
            // Use the setting for rotation
            RotateAndCreateBackup(options.OscamServerPath, options.NumberOfBackupsToKeep);

            using var stream = new FileStream(options.OscamServerPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.ASCII);

            foreach (var reader in uniqueReaders)
            {
                await writer.WriteAsync(reader.ToString());
            }

            Log.Information("Successfully wrote {Count} readers to {FilePath}. Backups rotated (max: {Max}).",
                uniqueReaders.Count, options.OscamServerPath, options.NumberOfBackupsToKeep);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error writing to oscam.server file at {FilePath}", options.OscamServerPath);
            throw;
        }
        finally
        {
            FileLock.Release();
        }
    }

    private void RotateAndCreateBackup(string originalPath, int maxBackups)
    {
        if (!File.Exists(originalPath) || maxBackups <= 0) return;

        try
        {
            // Shift existing backups: e.g., bak9 -> bak10, bak8 -> bak9...
            for (int i = maxBackups - 1; i >= 1; i--)
            {
                string oldBackup = $"{originalPath}.bak{i}";
                string newBackup = $"{originalPath}.bak{i + 1}";

                if (File.Exists(oldBackup))
                {
                    if (File.Exists(newBackup)) File.Delete(newBackup);
                    File.Move(oldBackup, newBackup);
                }
            }

            string firstBackup = $"{originalPath}.bak1";
            if (File.Exists(firstBackup)) File.Delete(firstBackup);
            File.Copy(originalPath, firstBackup);
        }
        catch (Exception ex)
        {
            Log.Warning("Rolling backup failed for {Path}: {Message}", originalPath, ex.Message);
        }
    }
}