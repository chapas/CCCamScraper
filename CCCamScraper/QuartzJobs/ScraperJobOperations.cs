using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Serilog;

namespace CCCamScraper.QuartzJobs
{
    internal static class ScraperJobOperations
    {
        public static List<OsCamReader> ParseCLines(List<string> cLines, string url)
        {
            var cccamLines = new List<CcCamLine>();

            cccamLines.AddRange(cLines.Select(ParseCLine));

            var readers = new List<OsCamReader>();

            readers.AddRange(cccamLines.Where(cl => cl != null)
                .Select(cl => new OsCamReader
                {
                    Device = cl.Hostname,
                    Port = cl.Port,
                    User = cl.Username,
                    Password = cl.Password,
                    Label = cl.Hostname,
                    Cccversion = cl.Cccversion,
                    Cccwantemu = cl.Wantemus,
                    Description = "0;0;0;0;" + cl.Username
                }));

            Log.Information($"Parsed {readers.Count} C lines from a total of {cLines.Count} found on {url}");

            return readers;
        }

        private static CcCamLine ParseCLine(string cline)
        {
            const string cPrefix = "c:";
            const char spaceChar = ' ';

            cline = cline.ToLowerInvariant();

            if (!cline.StartsWith(cPrefix))
                return null;

            var line = new CcCamLine();

            // Extracting the CCCVersion
            int lastIndexOfCardinal = cline.LastIndexOf('#');
            if (lastIndexOfCardinal != -1)
            {
                string versionSubstring = cline.Substring(lastIndexOfCardinal + 1).Trim().Replace("v", "");
                line.Cccversion = versionSubstring.Split('-')[0];
                cline = cline.Substring(0, lastIndexOfCardinal - 1).Trim();
            }

            // Removing C: or c: and splitting by space
            string[] s = cline.Substring(cPrefix.Length).Trim().Split(spaceChar);

            line.Hostname = s[0];
            line.Port = s[1];
            line.Username = s[2];
            line.Password = s[3];

            // Setting wantemus with a default value
            //line.Wantemus = s.Length > 4 && s[4] != "no" ? "yes" : "no";

            return line;
        }

        public static async Task<List<OscamUiStatusLine>> GetListWithCurrentServerStatusFromOsCam(
            string osCamStatusPageUrl)
        {
            //We need to add the browser headers
            var req = new DefaultHttpRequester();
            req.Headers["User-Agent"] =
                @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0";

            // Load default configuration
            var config = Configuration.Default.With(req).WithDefaultLoader()
                .WithDefaultCookies(); // Create a new browsing context
            var context =
                BrowsingContext
                    .New(config); // This is where the HTTP request happens, returns <IDocument> that // we can query later
            var document = context.OpenAsync(osCamStatusPageUrl).Result; // Log the data to the console
            //var asdf = document.DocumentElement.OuterHtml;
            // var docu = document.

            var rows = document.QuerySelectorAll("table.status tbody#tbodyp tr");

            var oscamUIStatusLine = new List<OscamUiStatusLine>();

            oscamUIStatusLine.AddRange(rows.Where(sl => sl != null)
                .Select(sl => new OscamUiStatusLine
                {
                    Description = ((IHtmlTableDataCellElement)sl.QuerySelectorAll("td.statuscol4").FirstOrDefault())
                        ?.Title?.Substring(
                            ((IHtmlTableDataCellElement)sl.QuerySelectorAll("td.statuscol4").FirstOrDefault()).Title
                            .LastIndexOf('\r') + 2)?.TrimEnd(')'),
                    ReaderUser = sl.QuerySelectorAll("td.statuscol4").Select(tg => tg.TextContent).FirstOrDefault()
                        ?.Trim(),
                    Port = sl.QuerySelectorAll("td.statuscol8").Select(tg => tg.TextContent).FirstOrDefault()?.Trim(),
                    LbValueReader = sl.QuerySelectorAll("td.statuscol14").Select(tg => tg.TextContent).FirstOrDefault()
                        ?.Trim(),
                    Status = sl.QuerySelectorAll("td.statuscol16").Select(tg => tg.TextContent).FirstOrDefault()?.Trim()
                }));

            oscamUIStatusLine.RemoveAll(line => line.ReaderUser == null || line.Port == null || line.Status == null);

            return oscamUIStatusLine;
        }

        public static Task<List<OsCamReader>> GetListWithCurrentReadersOnOscamServerFile(string oscamServerFilepath)
        {
            var lista = new List<OsCamReader>();
            var reader = new OsCamReader();
            var counter = 0;

            if (!File.Exists(oscamServerFilepath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(oscamServerFilepath) ?? throw new InvalidOperationException());
                File.Create(oscamServerFilepath).Dispose();
            }

            try
            {
                using (var sr = new StreamReader(oscamServerFilepath))
                {
                    foreach (var linha in File.ReadAllLines(oscamServerFilepath))
                    {
                        Debug.WriteLine(linha);

                        if (linha.StartsWith('#') || linha.StartsWith("/r/n") || string.IsNullOrEmpty(linha))
                            continue;

                        var arrayCCCAMLines = linha.Split("=");

                        switch (arrayCCCAMLines[0].Trim().ToLower())
                        {
                            case "[reader]":
                            {
                                if (!string.IsNullOrEmpty(reader.Label))
                                {
                                    lista.Add(reader);
                                    counter++;
                                }

                                reader = new OsCamReader();
                                continue;
                            }
                            case "label":
                                reader.Label = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Label
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "description":
                                reader.Description = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Description
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "enable":
                                reader.Enable = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Enable
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "protocol":
                                reader.Protocol = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Protocol
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "device":
                            {
                                if (!string.IsNullOrEmpty(arrayCCCAMLines[1]))
                                {
                                    var device = arrayCCCAMLines[1].Split(',');
                                    reader.Device = string.IsNullOrEmpty(device[0]) ? reader.Device : device[0].Trim();
                                    reader.Port = string.IsNullOrEmpty(device[1]) ? reader.Port : device[1].Trim();
                                }

                                continue;
                            }
                            case "key":
                                reader.Key = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Key
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "user":
                                reader.User = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.User
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "password":
                                reader.Password = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Password
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "inactivitytimeout":
                                reader.InactivityTimeout = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.InactivityTimeout
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "group":
                                reader.Group = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Group
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "cccversion":
                                reader.Cccversion = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Cccversion
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "ccckeepalive":
                                reader.Ccckeepalive = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Ccckeepalive
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "reconnecttimeout":
                                reader.ReconnectTimeout = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.ReconnectTimeout
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "lb_weight":
                                reader.LbWeight = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.LbWeight
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "cccmaxhops":
                                reader.Cccmaxhops = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Cccmaxhops
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            case "cccwantemu":
                                reader.Cccwantemu = string.IsNullOrEmpty(arrayCCCAMLines[1])
                                    ? reader.Cccwantemu
                                    : arrayCCCAMLines[1].Trim();
                                continue;
                            default:
                                Console.WriteLine("Skiped " + linha);
                                continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading from oscam.server file");
            }

            Log.Information("Got " + lista.Count + " readers from oscam.server file");

            return Task.FromResult(lista);
        }

        //public static async Task<List<OsCamReader>> RemoveReadersThatDontHaveTheCAID(List<OsCamReader> currentListOfCcCamReadersFromFile, List<OscamUiStatusLine> currentServerStatusList, CCCamScraperOptions scraperOptions)
        //{
        //    var readersToRemove = new List<OsCamReader>();

        //    //NEW VERSION, just deletes reader if CAID is not available
        //    foreach (var osCAMReader in currentListOfCcCamReadersFromFile)
        //    {
        //        var readerHasCaidFromUserAllowedCaids = await HasTheReaderAUserDefinedCaid(scraperOptions.OsCamReaderAPIURL + @"?part=entitlement&label=" + osCAMReader.Label, scraperOptions.CAIDs).ConfigureAwait(false);
        //        ///Let's look for the CAID and if it's there we don't do anything

        //        if (readerHasCaidFromUserAllowedCaids)
        //            continue;

        //        if(scraperOptions.ExcludedFromDeletion.Contains(osCAMReader.Label))
        //            continue;

        //        readersToRemove.Add(osCAMReader);
        //        Log.Information(osCAMReader.Label + " does not have a valid CAID and is flagged to be deleted");
        //    }

        //    if (readersToRemove.Count > 0)
        //        currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList();

        //    return currentListOfCcCamReadersFromFile;
        //}

        //public static async Task<List<OsCamReader>> RemoveReadersThatHaveUnwantedStatus(List<OsCamReader> currentListOfCcCamReadersFromFile, List<OscamUiStatusLine> currentServerStatusList, CCCamScraperOptions scraperOptions)
        //{
        //    var readersToRemove = new List<OsCamReader>();

        //    foreach (var osCAMUIReader in currentServerStatusList)
        //    {
        //        if (scraperOptions.UnwantedStatus.Contains(osCAMUIReader.Status))
        //        {
        //            var reader = currentListOfCcCamReadersFromFile.Where(camReader => camReader.Label == osCAMUIReader.ReaderUser);  

        //            readersToRemove.AddRange(reader);

        //            Log.Information(osCAMUIReader.ReaderUser + " with status " + osCAMUIReader.Status + " is flagged to be deleted.");
        //        }
        //    }

        //    if (readersToRemove.Count > 0)
        //        currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList();

        //    return currentListOfCcCamReadersFromFile;
        //}

        //private static async Task<bool> HasTheReaderAUserDefinedCaid(string osCamReaderPageUrl, string[] caiDs)
        //{
        //    try
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(oscam));
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.BaseAddress = new Uri(osCamReaderPageUrl);
        //            httpClient.DefaultRequestHeaders.Accept.Clear();
        //            httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");

        //            var response = await httpClient.GetAsync(osCamReaderPageUrl).ConfigureAwait(false);

        //            response.EnsureSuccessStatusCode();

        //            if (response.IsSuccessStatusCode)
        //            {
        //                using (StringReader reader = new StringReader(await response.Content.ReadAsStringAsync().ConfigureAwait(false)))
        //                {
        //                    var test = (oscam)serializer.Deserialize(reader);

        //                    var totalCardCount = test.reader?.Select(oscamReader => oscamReader)
        //                                             .FirstOrDefault()
        //                                             ?.cardlist.FirstOrDefault()
        //                                             ?.totalcards;

        //                    if (totalCardCount == null || int.Parse(totalCardCount) == 0)
        //                        return false;

        //                    if (caiDs.Any())
        //                        foreach (string caid in caiDs)
        //                        {
        //                            var hasCaid = (test.reader.Select(oscamReader => oscamReader)
        //                                               .FirstOrDefault()?
        //                                               .cardlist.FirstOrDefault()?
        //                                               .card)
        //                                .FirstOrDefault(card => card.caid.Contains(caid));

        //                            if (hasCaid != null)
        //                                return true;
        //                        }
        //                    else
        //                        return true;

        //                    return false;
        //                }
        //            }

        //            Log.Error($"Didn't had access to the oscam reader details page: {osCamReaderPageUrl}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"Didn't had access to the oscam reader details page: {osCamReaderPageUrl}");
        //        return true; // this is a bit special, if it throws we don't care and continue to the next line
        //    }

        //    return false;
        //}

        public static List<OsCamReader> AddNewScrapedReaders(List<OsCamReader> currentServerReaders,
            List<OsCamReader> newlyScrapedReaders)
        {
            var newReaders = new List<OsCamReader>();

            foreach (var line in newlyScrapedReaders)
            {
                var OnFile = false;
                foreach (var currentlines in currentServerReaders)
                    if ((line.Device == currentlines.Device) &
                        (line.Port == currentlines.Port) &
                        (line.User == currentlines.User))
                    {
                        OnFile = true;
                        break;
                    }

                if (!OnFile)
                    newReaders.Add(line);
            }

            currentServerReaders.AddRange(newReaders);

            Log.Information("Added " + newReaders.Count + " new readers to oscam.server");

            return currentServerReaders;
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
                    reader.Label = reader.Label + Guid.NewGuid().ToString().Split('-')[0];

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


        public static async Task<List<string>> ScrapeCLinesFromUrl(CCCamScraperJobOption quartzJobsOptions)
        {
            var urlToScrapeFrom = UrlStringReplacement(quartzJobsOptions.URLToScrape);

            Log.Information($"Started scraping on {urlToScrapeFrom}");

            //We need to add the browser headers
            var req = new DefaultHttpRequester();
            req.Headers["User-Agent"] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0";
//            req.Headers["authority"] = @"cccamiptv.club";
//            req.Headers["accept"] = @"text / html, application / xhtml + xml, application / xml; q = 0.9,image / avif,image / webp,image / apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
//req.Headers["accept-language"] = @"en,pt;q=0.9,pt-PT;q=0.8,en-GB;q=0.7,en-US;q=0.6,fr;q=0.5,es;q=0.4";
//req.Headers["cache-control"] = @"no-cache";
//req.Headers["cookie"] = @"_ga=GA1.2.132141673.1653296901; trp_language=en_US; _gid=GA1.2.1432885813.1653912358; crisp-client^%^2Fsession^%^2F2bd78d93-2a96-4942-b9a2-84799037f903=session_11da2a9d-c79f-418a-98f2-1f86ad9b019b";
//req.Headers["dnt"] = @"1";
//req.Headers["pragma"] = @"no-cache";
//req.Headers["referer"] = @"https://cccamiptv.club/";
//req.Headers["sec-ch-ua"] = @"^\^"" Not A;Brand^\^"";v=^\^""99^\^"", ^\^""Chromium^\^"";v=^\^""102^\^"", ^\^""Google Chrome^\^"";v=^\^""102^\^""";
//req.Headers["sec-ch-ua-mobile"] = @"?0";
//req.Headers["sec-ch-ua-platform"] = @"^\^""Windows^\^""";
//req.Headers["sec-fetch-dest"] = @"document";
//req.Headers["sec-fetch-mode"] = @"navigate";
//req.Headers["sec-fetch-site"] = @"same-origin";
//req.Headers["sec-fetch-user"] = @"?1";
//req.Headers["upgrade-insecure-requests"] = @"1";
//req.Headers["user-agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.63 Safari/537.36";


            // Load default configuration
            var config = Configuration.Default.With(req).WithDefaultLoader()
                .WithDefaultCookies(); // Create a new browsing context
            //var context = BrowsingContext.New(config); // This is where the HTTP request happens, returns <IDocument> that // we can query later
            
            var document = await BrowsingContext.New(config).OpenAsync(urlToScrapeFrom); // Log the data to the console

            HashSet<string> uniqueCStrings = new HashSet<string>();

            // Traverse the DOM and find strings starting with 'c: ' in every element
            foreach (var element in document.All)
            {
                string textContent = element.TextContent.Trim();

                // Check if the text content starts with 'c: '
                if (textContent.StartsWith("c: ", StringComparison.OrdinalIgnoreCase))
                {
                    // Add to the HashSet to ensure uniqueness
                    uniqueCStrings.Add(textContent);
                }
            }

            // Convert the HashSet to a List
            return new List<string>(uniqueCStrings);


            //firewall blocking this will yield ZERO lines (damn)
            var lines = document.QuerySelectorAll(quartzJobsOptions!.ScrapePath)
                .Select(m => m.InnerHtml
                    .Replace("<br>", "")
                    .Replace("</p>", "")
                    .Trim()
                    .Split("\n"));

            var cLines = new List<string>();

            try
            {
                cLines = lines?.ToList()[0].Where(line => line.ToLower().Trim().StartsWith("c:")).ToList();

                if (cLines.Any())
                {
                    Log.Information($"Scraped {cLines.Count()} C lines from {urlToScrapeFrom}");
                    return cLines;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error getting C lines from {urlToScrapeFrom} <------ Is this a valid URL?");
                return cLines;
            }

            Log.Warning($"Scraped ZERO C lines from {urlToScrapeFrom}");
            return cLines;
        }

        internal static string UrlStringReplacement(string url)
        {
            if (!(url.Contains('<') & url.Contains('>')))
                return url;

            var _day = DateTime.Today.Day.ToString("00", CultureInfo.InvariantCulture);
            var _month = DateTime.Today.Month.ToString("00", CultureInfo.InvariantCulture);
            var _year = DateTime.Today.Year.ToString("0000", CultureInfo.InvariantCulture);

            url = url.Replace("<yyyy>", _year);
            url = url.Replace("<mm>", _month);
            url = url.Replace("<dd>", _day);

            return url;
        }
    }
}