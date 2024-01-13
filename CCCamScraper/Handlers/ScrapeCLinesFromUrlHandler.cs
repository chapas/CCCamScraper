using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Quartz;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Io;
using CCCamScraper.Configurations;
using Serilog;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;

namespace CCCamScraper.Handlers
{
    public class ScrapeCLinesFromUrlHandler : IHandler
    {
        private readonly string urlToScrape;
        private readonly IOptionsMonitor<CCCamScraperOptions> _ccCamScraperOptions;
        private IHandler _nextHandler;

        public ScrapeCLinesFromUrlHandler(
            string urlToScrape,
            IOptionsMonitor<CCCamScraperOptions> ccCamScraperOptions)
        {
            this.urlToScrape = urlToScrape;
            _ccCamScraperOptions = ccCamScraperOptions;
        }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return _nextHandler;
        }

        public async Task<object> Handle(IJobExecutionContext context)
        {
            var osCamReaders = new HashSet<OsCamReader>((context.Result as List<OsCamReader>)!, new OsCamReaderComparer());
            var scrappedLines = await ScrapeCLinesFromUrl(urlToScrape).ConfigureAwait(false);
            osCamReaders.UnionWith(ParseCLines(scrappedLines, context.JobDetail.Key.Name));
            context.Result = osCamReaders.ToList();

            return _nextHandler.Handle(context);
        }

        private async Task<List<string>> ScrapeCLinesFromUrl(string urlToScrape)
        {
            var urlToScrapeFrom = UrlStringReplacement(urlToScrape);
            Log.Information($"Started scraping on {urlToScrapeFrom}");

            var req = new DefaultHttpRequester();
            req.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0";

            var config = Configuration.Default.With(req).WithDefaultLoader().WithDefaultCookies();
            var document = await BrowsingContext.New(config).OpenAsync(urlToScrapeFrom);

            var uniqueCStrings = document.All
                .Where(element => element.TextContent.Trim().StartsWith("c: ", StringComparison.OrdinalIgnoreCase))
                .Select(element => element.TextContent.Trim())
                .ToHashSet();

            return SplitLinesIntoHashSet(uniqueCStrings).ToList();
        }

        private static string UrlStringReplacement(string url)
        {
            if (!(url.Contains('<') & url.Contains('>')))
                return url;

            var day = DateTime.Today.AddDays(-1).Day.ToString("00", CultureInfo.InvariantCulture);
            var month = DateTime.Today.Month.ToString("00", CultureInfo.InvariantCulture);
            var year = DateTime.Today.Year.ToString("0000", CultureInfo.InvariantCulture);

            url = url.Replace("<yyyy>", year);
            url = url.Replace("<mm>", month);
            url = url.Replace("<dd>", day);

            return url;
        }

        private List<OsCamReader> ParseCLines(List<string> cLines, string url)
        {
            var cccamLines = cLines.Select(ParseCLine).Where(cl => cl != null).ToList();

            var readers = cccamLines.Select(cl => new OsCamReader
            {
                Device = cl.Hostname,
                Port = cl.Port,
                User = cl.Username,
                Password = cl.Password,
                Label = cl.Hostname,
                Cccversion = cl.Cccversion,
                Cccwantemu = cl.Wantemus,
                Description = "0;0;0;0;" + cl.Username,
                Caid = string.Join(",", _ccCamScraperOptions.CurrentValue.CaiDs),
            }).ToList();

            Log.Information($"Parsed {readers.Count} C lines from a total of {cLines.Count} found on {url}");

            return readers;
        }

        private static CcCamLine ParseCLine(string cline)
        {
            const string cPrefix = "c:";
            const char spaceChar = ' ';

            cline = cline.ToLowerInvariant();

            if (!cline.StartsWith(cPrefix))
                return null!;

            var line = new CcCamLine();

            int lastIndexOfCardinal = cline.LastIndexOf('#');
            if (lastIndexOfCardinal != -1)
            {
                string versionSubstring = cline.Substring(lastIndexOfCardinal + 1).Trim().Replace("v", "");
                line.Cccversion = versionSubstring.Split('-')[0];
                cline = cline.Substring(0, lastIndexOfCardinal - 1).Trim();
            }

            string[] s = cline.Substring(cPrefix.Length).Trim().Split(spaceChar);

            line.Hostname = s[0];
            line.Port = s[1];
            line.Username = s[2];
            line.Password = s[3];

            return line;
        }

        public HashSet<string> SplitLinesIntoHashSet(HashSet<string> inputSet)
        {
            if (inputSet.Count > 1)
                return inputSet;
            
            var input = inputSet.First();

            if (input.Count(sub => sub == 'C') > 1)
            {
                var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                return new HashSet<string>(lines.Where(line => line.StartsWith("C: ", StringComparison.OrdinalIgnoreCase)));
            }

            return new HashSet<string> { input };
        }
    }

    public class OsCamReaderComparer : IEqualityComparer<OsCamReader>
    {
        public bool Equals(OsCamReader x, OsCamReader y)
        {
            return x.Device == y.Device
                   && x.User == y.User
                   && x.Password == y.Password;
        }

        public int GetHashCode(OsCamReader obj)
        {
            // Return a hash code based on the values you're comparing in the Equals method.
            // For example, if you're comparing based on the Label property:
            return obj?.Label?.GetHashCode() ?? 0;
        }
    }
}