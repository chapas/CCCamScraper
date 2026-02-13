using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using CCCamScraper.Models;
using Serilog;
using System.Reflection;

namespace CCCamScraper;

/// <summary>
/// Provides operations for scraping OSCam server status and managing reader configurations.
/// </summary>
internal static class ScraperJobOperations
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0";

    /// <summary>
    /// Retrieves the list of readers from the OSCam readers page using the dataTable structure.
    /// </summary>
    public static async Task<List<OscamUIReaderStatus>> GetListWithCurrentReadersStatusFromOsCamPage(string osCamReadersPageUrl)
    {
        if (string.IsNullOrEmpty(osCamReadersPageUrl))
        {
            throw new ArgumentNullException(nameof(osCamReadersPageUrl), "URL cannot be null.");
        }

        var config = Configuration.Default
            .With(new DefaultHttpRequester { Headers = { ["User-Agent"] = "Mozilla/5.0" } })
            .WithDefaultLoader();

        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(osCamReadersPageUrl);

        // Target the table with ID 'dataTable' which is present in your uploaded file
        // We select rows inside the tbody to avoid the header <thead>
        var rows = document.QuerySelectorAll("table#dataTable tbody tr");

        var readersList = rows.Select(row =>
        {
            // Reader Name is in class 'readercol1'
            var readerName = row.QuerySelector("td.readercol1")?.TextContent?.Trim();

            // If there's no reader name, it's likely a separator or footer row
            if (string.IsNullOrEmpty(readerName)) return null;

            return new OscamUIReaderStatus
            {
                // Column 0: On/Off (usually contains an input or image)
                OnOff = row.QuerySelector("td.readercol0")?.TextContent?.Trim() ?? string.Empty,

                Reader = readerName,

                // Column 2: Protocol
                Protocol = row.QuerySelector("td.readercol2")?.TextContent?.Trim() ?? string.Empty,

                // Column 20 contains BOTH Status and Address
                // Usually looks like: "online <br> 1.2.3.4"
                Status = row.QuerySelector("td.readercol20 b")?.TextContent?.Trim() ?? string.Empty,

                // Extract address by taking the text after the <b> tag in col 20
                Address = row.QuerySelector("td.readercol20")?.ChildNodes
                            .FirstOrDefault(n => n.NodeType == AngleSharp.Dom.NodeType.Text && !string.IsNullOrWhiteSpace(n.TextContent))
                            ?.TextContent?.Trim() ?? string.Empty,

                // ECM Statistics - column classes 4, 5, and 6
                OK = ParseEcmValue(row.QuerySelector("td.readercol4")?.TextContent),
                NOK = ParseEcmValue(row.QuerySelector("td.readercol5")?.TextContent),
                TOut = ParseEcmValue(row.QuerySelector("td.readercol6")?.TextContent)
            };
        })
        .Where(r => r != null)
        .Cast<OscamUIReaderStatus>()
        .ToList();

        Log.Information("Fetched {Count} readers from {Url} using dataTable selectors.", readersList.Count, osCamReadersPageUrl);
        return readersList;
    }

    /// <summary>
    /// Extracts the integer part from strings like "61 (100%)" or "0 (0%)"
    /// </summary>
    private static int ParseEcmValue(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return 0;

        // Split at the first space to remove "(percentage%)"
        string cleanValue = rawValue.Trim().Split(' ')[0];

        return int.TryParse(cleanValue, out int result) ? result : 0;
    }

    /// <summary>
    /// Retrieves the current server status from an OSCam status page.
    /// </summary>
    /// <param name="osCamStatusPageUrl">The URL of the OSCam status page.</param>
    /// <returns>A list of <see cref="OscamUiStatusLine"/> objects representing the server status.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="osCamStatusPageUrl"/> is null or empty.</exception>
    public static async Task<List<OscamUiStatusLine>> GetListWithCurrentServerStatusFromOsCamStatusPage(string osCamStatusPageUrl)
    {
        if (string.IsNullOrEmpty(osCamStatusPageUrl))
        {
            throw new ArgumentNullException(nameof(osCamStatusPageUrl), "OSCam status page URL cannot be null or empty.");
        }

        var config = Configuration.Default
            .With(new DefaultHttpRequester { Headers = { ["User-Agent"] = UserAgent } })
            .WithDefaultLoader()
            .WithDefaultCookies();

        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(osCamStatusPageUrl);

        var rows = document.QuerySelectorAll("table.status tbody#tbodyp tr");

        var osCamUiStatusLine = rows
            .Select(row => new OscamUiStatusLine
            {
                Description = row.QuerySelector("td.statuscol4") is IHtmlTableDataCellElement descriptionElement
                    ? descriptionElement.Title?[(descriptionElement.Title.LastIndexOf('\r') + 2)..]?.TrimEnd(')')
                    : null,
                ReaderUser = row.QuerySelector("td.statuscol4")?.TextContent?.Trim(),
                Port = row.QuerySelector("td.statuscol8")?.TextContent?.Trim(),
                LbValueReader = row.QuerySelector("td.statuscol14")?.TextContent?.Trim(),
                Status = row.QuerySelector("td.statuscol16")?.TextContent?.Trim()
            })
            .Where(line => !string.IsNullOrEmpty(line.ReaderUser) && !string.IsNullOrEmpty(line.Port) && !string.IsNullOrEmpty(line.Status))
            .ToList();

        Log.Information("Fetched {Count} status lines from OSCam status page at {Url}", osCamUiStatusLine.Count, osCamStatusPageUrl);
        return osCamUiStatusLine;
    }

    public static async Task<List<OsCamReader>> GetListWithCurrentReadersOnOsCamServerFile(string oscamServerFilepath)
    {
        if (string.IsNullOrWhiteSpace(oscamServerFilepath))
        {
            throw new ArgumentNullException(nameof(oscamServerFilepath), "Path cannot be null.");
        }

        var osCamReaders = new List<OsCamReader>();
        OsCamReader? currentReader = null;

        try
        {
            if (!File.Exists(oscamServerFilepath))
            {
                // Robust directory creation
                var dir = Path.GetDirectoryName(oscamServerFilepath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(oscamServerFilepath, string.Empty);
                return osCamReaders;
            }

            // Use ReadLines to be memory efficient if the file grows large
            foreach (var line in File.ReadLines(oscamServerFilepath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#')) continue;

                // Start of new reader section
                if (trimmedLine.Equals("[reader]", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentReader?.Label != null) osCamReaders.Add(currentReader);
                    currentReader = new OsCamReader();
                    continue;
                }

                if (currentReader == null) continue;

                var parts = trimmedLine.Split('=', 2);
                if (parts.Length < 2) continue;

                var key = parts[0].Trim().ToLower();
                var value = parts[1].Trim();

                // SPECIAL CASE HANDLING
                switch (key)
                {
                    case "description":
                        // Safely parse the complex description string 
                        currentReader.Description = ParseDescriptionString(value);
                        break;

                    case "device":
                        var deviceParts = value.Split(',', 2);
                        currentReader.Device = deviceParts[0].Trim();
                        if (deviceParts.Length > 1) currentReader.Port = deviceParts[1].Trim();
                        break;

                    default:
                        // Fallback for standard string properties
                        try
                        {
                            var prop = typeof(OsCamReader).GetProperty(key,
                                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                            // Ensure we aren't trying to overwrite the complex 'Description' object with a string
                            if (prop != null && prop.PropertyType == typeof(string))
                            {
                                prop.SetValue(currentReader, value);
                            }
                        }
                        catch { /* Ignore bad/non-existent properties for stability */ }
                        break;
                }
            }

            // Add the final reader in the file
            if (currentReader?.Label != null) osCamReaders.Add(currentReader);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse OSCam server file at {Path}", oscamServerFilepath);
        }

        return osCamReaders;
    }

    /// <summary>
    /// Robustly parses the semicolon-separated description string.
    /// Format expected: Error;Off;Unknown;LbValueReader;Username;ECMOK;ECMNOK;ECMTOUT
    /// </summary>
    private static OsCamReaderDescription ParseDescriptionString(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return new OsCamReaderDescription();

        // Split based on the semicolon separator found in oscam.server
        var segments = rawValue.Split(';');

        // We parse values into variables first to handle missing segments gracefully
        uint err = segments.Length > 0 ? ParseUint(segments[0]) : 0;
        uint off = segments.Length > 1 ? ParseUint(segments[1]) : 0;
        uint unk = segments.Length > 2 ? ParseUint(segments[2]) : 0;
        uint lbv = segments.Length > 3 ? ParseUint(segments[3]) : 0;
        string user = segments.Length > 4 ? segments[4].Trim() : string.Empty;
        uint ok = segments.Length > 5 ? ParseUint(segments[5]) : 0;
        uint nok = segments.Length > 6 ? ParseUint(segments[6]) : 0;
        uint tout = segments.Length > 7 ? ParseUint(segments[7]) : 0;

        // Create the object using the object initializer (standard for classes)
        return new OsCamReaderDescription
        {
            AccumulatedError = err,
            AccumulatedOff = off,
            AccumulatedUnknown = unk,
            LbValueReader = lbv,
            Username = user,
            ECMOK = ok,
            ECMNOK = nok,
            ECMTOUT = tout
        };
    }

    private static uint ParseUint(string input)
    {
        return uint.TryParse(input, out var result) ? result : 0;
    }
}