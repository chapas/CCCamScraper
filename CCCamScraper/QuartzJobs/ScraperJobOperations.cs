using System.Reflection;
using System.Text;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using CCCamScraper.Models;
using Serilog;

namespace CCCamScraper;

internal static class ScraperJobOperations
{
    public static async Task<List<OscamUiStatusLine>> GetListWithCurrentServerStatusFromOsCamStatusPage(
        string osCamStatusPageUrl)
    {
        //We need to add the browser headers
        var req = new DefaultHttpRequester
        {
            Headers =
            {
                ["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0"
            }
        };

        // Load default configuration
        var config = Configuration.Default.With(req).WithDefaultLoader().WithDefaultCookies(); // Create a new browsing context
        var context = BrowsingContext.New(config);

        // This is where the HTTP request happens, returns <IDocument> that we can query later
        var document = await context.OpenAsync(osCamStatusPageUrl);

        var rows = document.QuerySelectorAll("table.status tbody#tbodyp tr");

        var osCamUiStatusLine = rows.Where(sl => sl != null)
            .Select(sl =>
            {
                var descriptionElement = (IHtmlTableDataCellElement)sl.QuerySelector("td.statuscol4");
                var description = descriptionElement?.Title?.Substring(descriptionElement.Title.LastIndexOf('\r') + 2)?.TrimEnd(')');
                return new OscamUiStatusLine
                {
                    Description = description,
                    ReaderUser = sl.QuerySelector("td.statuscol4")?.TextContent?.Trim(),
                    Port = sl.QuerySelector("td.statuscol8")?.TextContent?.Trim(),
                    LbValueReader = sl.QuerySelector("td.statuscol14")?.TextContent?.Trim(),
                    Status = sl.QuerySelector("td.statuscol16")?.TextContent?.Trim()
                };
            })
            .Where(line => line.ReaderUser != null && line.Port != null && line.Status != null)
            .ToList();

        return osCamUiStatusLine;
    }

    private static readonly object FileLock = new ();

    public static Task<List<OsCamReader>> GetListWithCurrentReadersOnOsCamServerFile(string oscamServerFilepath)
    {
        var osCamReaders = new List<OsCamReader>();
        var reader = new OsCamReader();

        if (!File.Exists(oscamServerFilepath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(oscamServerFilepath) ?? throw new InvalidOperationException());
            File.Create(oscamServerFilepath).Dispose();
        }

        try
        {
            lock (FileLock)
            {
                var lines = File.ReadAllLines(oscamServerFilepath);

                foreach (var linha in lines)
                {
                    if (linha.StartsWith('#') || linha.StartsWith("/r/n") || string.IsNullOrEmpty(linha))
                        continue;

                    var arrayCccamLines = linha.Split("=");

                    switch (arrayCccamLines[0].Trim().ToLower())
                    {
                        case "[reader]":
                        {
                            if (!string.IsNullOrEmpty(reader.Label))
                            {
                                osCamReaders.Add(reader);
                            }

                            reader = new OsCamReader();
                            continue;
                        }
                        case "device":
                        {
                            if (!string.IsNullOrEmpty(arrayCccamLines[1]))
                            {
                                var device = arrayCccamLines[1].Split(',');
                                reader.Device = string.IsNullOrEmpty(device[0]) ? reader.Device : device[0].Trim();
                                reader.Port = string.IsNullOrEmpty(device[1]) ? reader.Port : device[1].Trim();
                            }

                            continue;
                        }
                        default:
                        {
                            if (arrayCccamLines.Length > 1 && !string.IsNullOrEmpty(arrayCccamLines[1]))
                            {
                                var property = typeof(OsCamReader).GetProperty(arrayCccamLines[0].Trim(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                if (property != null)
                                {
                                    property.SetValue(reader, arrayCccamLines[1].Trim());
                                }
                            }

                            continue;
                        }
                    }
                }

            }

            if (!string.IsNullOrEmpty(reader.Label))
                osCamReaders.Add(reader);

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reading from oscam.server file");
        }

        Log.Information("Fetched " + osCamReaders.Count + " readers from 'oscam.server' file");

        return Task.FromResult(osCamReaders);
    }

    public static void WriteOsCamReadersToFile(List<OsCamReader> currentServerStatusList,
        string oscamServerFilepath = @"oscam.server")
    {
        // Define a static object for locking
        object fileLock = new object();

        var readers = new List<OsCamReader>();

        foreach (var reader in currentServerStatusList)
        {
            if (readers.FirstOrDefault(camReader => camReader.Label.Contains(reader.Label)) != null)
                reader.Label = $"{reader.Label}_({reader.User})";

            readers.Add(reader);
        }

        // Use lock to ensure exclusive access to the file
        lock (fileLock)
        {
            using (var sr = new StreamWriter(oscamServerFilepath, false, Encoding.ASCII))
            {
                foreach (var reader in readers)
                    sr.Write(reader.ToString());
            }
        }

        Log.Information("Wrote a total of " + currentServerStatusList.Count + " readers to oscam.server");
    }
}