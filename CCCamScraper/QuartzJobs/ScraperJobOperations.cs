using AngleSharp;
using AngleSharp.Dom;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AngleSharp.Io;
using CCCamScraper.Configurations;
using CCCamScraper.Models;

namespace CCCamScraper
{
    internal static class ScraperJobOperations
    {
        public static List<OsCamReader> ParseCLines(List<string> cLines, string url)
        {
            List<CcCamLine> cccamLines = new List<CcCamLine>();

            cccamLines.AddRange(cLines.Select(ParseCLine));
            
            //foreach (var cline in cLines)
            //{
            //    try
            //    {
            //        var parsedLine = ParseCLine(cline);
            //        cccamlines.Add(parsedLine);
            //    }
            //    catch
            //    {
            //    }
            //}

            List<OsCamReader> readers = new List<OsCamReader>();

            readers.AddRange(cccamLines.Where(cl => cl != null)
                .Select(cl => new OsCamReader()
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

            Log.Information($"Parsed { readers.Count } C lines from a total of { cLines.Count } found on { url }");

            return readers;
        }

        private static CcCamLine ParseCLine(string cline)
        {
            if (!cline.ToString().StartsWith(@"C:"))
                return null;

            CcCamLine line = new CcCamLine();

            if (cline.LastIndexOf('#') != -1)
            {
                int lastIndexOfCardinal = cline.LastIndexOf('#');

                var c = cline.Substring(lastIndexOfCardinal + 1, cline.Length - lastIndexOfCardinal - 1).Trim().Replace("v", "");
                line.Cccversion = c.Remove(c.IndexOf("-"), c.Length - c.IndexOf("-"));

                cline = cline.Substring(0, cline.IndexOf("#") - 1).Trim();
            }

            var s = cline.Replace("C:", "").Replace("c:", "").Trim().Split(" ");

                line.Hostname = s[0];
                line.Port = s[1];
                line.Username = s[2];
                line.Password = s[3];


            //try
            //{
            //    line.wantemus = s[4] == "no" ? "no" : "yes";
            //}
            //catch { }

            return line;
        }

        public static async Task<List<OscamUiStatusLine>> GetListWithCurrentServerStatusFromOsCam(string osCamStatusPageUrl)
        {
            //We need to add the browser headers
            DefaultHttpRequester req = new DefaultHttpRequester();
            req.Headers["User-Agent"] = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0";

            // Load default configuration
            var config = Configuration.Default.With(req).WithDefaultLoader().WithDefaultCookies();       // Create a new browsing context
            var context = BrowsingContext.New(config);                            // This is where the HTTP request happens, returns <IDocument> that // we can query later
            IDocument document = context.OpenAsync(osCamStatusPageUrl).Result;    // Log the data to the console
                                                                                  //var asdf = document.DocumentElement.OuterHtml;
                                                                                  // var docu = document.

            var rows = document.QuerySelectorAll("table.status tbody#tbodyp tr");

            var oscamUIStatusLine = new List<OscamUiStatusLine>();

            oscamUIStatusLine.AddRange(rows.Where(sl => sl != null)
                           .Select(sl => new OscamUiStatusLine()
                           {
                               Description = ((AngleSharp.Html.Dom.IHtmlTableDataCellElement)sl.QuerySelectorAll("td.statuscol4").FirstOrDefault())?.Title?.Substring(((AngleSharp.Html.Dom.IHtmlTableDataCellElement)sl.QuerySelectorAll("td.statuscol4").FirstOrDefault()).Title.LastIndexOf('\r') + 2)?.TrimEnd(')'),
                               ReaderUser = sl.QuerySelectorAll("td.statuscol4").Select(tg => tg.TextContent).FirstOrDefault()?.Trim(),
                               Port = sl.QuerySelectorAll("td.statuscol8").Select(tg => tg.TextContent).FirstOrDefault()?.Trim(),
                               LbValueReader = sl.QuerySelectorAll("td.statuscol14").Select(tg => tg.TextContent).FirstOrDefault()?.Trim(),
                               Status = sl.QuerySelectorAll("td.statuscol16").Select(tg => tg.TextContent).FirstOrDefault()?.Trim()
                           }));

            oscamUIStatusLine.RemoveAll(line => line.ReaderUser == null || line.Port == null || line.Status == null);

            return oscamUIStatusLine;
        }

        public static Task<List<OsCamReader>> GetListWithCurrentReadersOnOscamServerFile(string oscamServerFilepath)
        {
            var lista = new List<OsCamReader>();
            OsCamReader reader = new OsCamReader();
            int counter = 0;

            try
            {
                using (StreamReader sr = new StreamReader(oscamServerFilepath))
                {
                    foreach (string linha in File.ReadAllLines(oscamServerFilepath))
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
                                reader.Label = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Label : arrayCCCAMLines[1].Trim();
                                continue;
                            case "description":
                                reader.Description = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Description : arrayCCCAMLines[1].Trim();
                                continue;
                            case "enable":
                                reader.Enable = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Enable : arrayCCCAMLines[1].Trim();
                                continue;
                            case "protocol":
                                reader.Protocol = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Protocol : arrayCCCAMLines[1].Trim();
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
                                reader.Key = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Key : arrayCCCAMLines[1].Trim();
                                continue;
                            case "user":
                                reader.User = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.User : arrayCCCAMLines[1].Trim();
                                continue;
                            case "password":
                                reader.Password = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Password : arrayCCCAMLines[1].Trim();
                                continue;
                            case "inactivitytimeout":
                                reader.Inactivitytimeout = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Inactivitytimeout : arrayCCCAMLines[1].Trim();
                                continue;
                            case "group":
                                reader.Group = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Group : arrayCCCAMLines[1].Trim();
                                continue;
                            case "cccversion":
                                reader.Cccversion = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Cccversion : arrayCCCAMLines[1].Trim();
                                continue;
                            case "ccckeepalive":
                                reader.Ccckeepalive = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Ccckeepalive : arrayCCCAMLines[1].Trim();
                                continue;
                            case "reconnecttimeout":
                                reader.Reconnecttimeout = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Reconnecttimeout : arrayCCCAMLines[1].Trim();
                                continue;
                            case "lb_weight":
                                reader.LbWeight = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.LbWeight : arrayCCCAMLines[1].Trim();
                                continue;
                            case "cccmaxhops":
                                reader.Cccmaxhops = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Cccmaxhops : arrayCCCAMLines[1].Trim();
                                continue;
                            case "cccwantemu":
                                reader.Cccwantemu = string.IsNullOrEmpty(arrayCCCAMLines[1]) ? reader.Cccwantemu : arrayCCCAMLines[1].Trim();
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
                Log.Error(ex,"Error while reading from oscam.server file");
            }

            Log.Information("Got " + lista.Count + " readers from oscam.server file");

            return Task.FromResult(lista);
        }

        public static async Task<List<OsCamReader>> RemoveReadersThatDontHaveTheCAID(List<OsCamReader> currentListOfCcCamReadersFromFile, List<OscamUiStatusLine> currentServerStatusList, CCCamScraperOptions scraperOptions)
        {
            var readersToRemove = new List<OsCamReader>();

            //NEW VERSION, just deletes reader if CAID is not available
            foreach (var osCAMReader in currentListOfCcCamReadersFromFile)
            {
                var readerHasCaidFromUserAllowedCaids = await HasTheReaderAUserDefinedCaid(scraperOptions.OsCamReaderAPIURL + @"?part=entitlement&label=" + osCAMReader.Label, scraperOptions.CAIDs).ConfigureAwait(false);
                ///Let's look for the CAID and if it's there we don't do anything
                
                if (readerHasCaidFromUserAllowedCaids)
                    continue;

                readersToRemove.Add(osCAMReader);
                Log.Information(osCAMReader.Label + " does not have a valid CAID and is flagged to be deleted");
            }

            if (readersToRemove.Count > 0)
                currentListOfCcCamReadersFromFile = currentListOfCcCamReadersFromFile.Except(readersToRemove).ToList();

            return currentListOfCcCamReadersFromFile;
        }

        private static async Task<bool> HasTheReaderAUserDefinedCaid(string osCamReaderPageUrl, string[] caiDs)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(oscam));
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(osCamReaderPageUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");

                    var response = await httpClient.GetAsync(osCamReaderPageUrl).ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        using (StringReader reader = new StringReader(await response.Content.ReadAsStringAsync().ConfigureAwait(false)))
                        {
                            var test = (oscam)serializer.Deserialize(reader);

                            var totalCardCount = test.reader?.Select(oscamReader => oscamReader)
                                                     .FirstOrDefault()
                                                     ?.cardlist.FirstOrDefault()
                                                     ?.totalcards;

                            if (totalCardCount == null || int.Parse(totalCardCount) == 0)
                                return false;

                            if (caiDs.Any())
                                foreach (string caid in caiDs)
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

        public static List<OsCamReader> AddNewScrapedReaders(List<OsCamReader> currentServerReaders, List<OsCamReader> newlyScrapedReaders)
        {
            var newReaders = new List<OsCamReader>();

            foreach (var line in newlyScrapedReaders)
            {
                var OnFile = false;
                foreach (var currentlines in currentServerReaders)
                {
                    if (line.Device == currentlines.Device &
                        line.Port == currentlines.Port &
                        line.User == currentlines.User)
                    {
                        OnFile = true;
                        break;
                    }
                }

                if (!OnFile)
                    newReaders.Add(line);
            }

            currentServerReaders.AddRange(newReaders);

            Log.Information("Added " + newReaders.Count + " new readers to oscam.server");

            return currentServerReaders;
        }

        public static void WriteOsCamReadersToFile(List<OsCamReader> currentServerStatusList, string oscamServerFilepath = @"oscam.server")
        {
            var readers = new List<OsCamReader>();

            foreach (var reader in currentServerStatusList)
            {
                if (readers.FirstOrDefault(camReader => camReader.Label.Contains(reader.Label)) != null)
                    reader.Label = reader.Label + Guid.NewGuid().ToString().Split('-')[0];

                readers.Add(reader);
            }

            using (StreamWriter sr = new StreamWriter(oscamServerFilepath, false, Encoding.ASCII))
            {
                foreach (var reader in readers)
                    sr.Write(reader.ToString());
            }

            Log.Information("Wrote a total of " + currentServerStatusList.Count + " readers to oscam.server");
        }
    }
}