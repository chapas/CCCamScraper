using AngleSharp;
using AngleSharp.Io;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace CCCamScraper.Handlers
{
    public class ScrapeCLinesFromUrlHandler : IHandler
    {
        private readonly string urlToScrape;
        private readonly IOptionsMonitor<CCCamScraperOptions> _ccCamScraperOptions;
        private IHandler _nextHandler;
        private const string cccamRegExPattern = @"[cC]: ([\w.-]+ )(\b\d{1,5}\b )([\w.-]+ )([\w.-]+)( # ?[\w.-]+)?";

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

            var uniqueCStrings = document.All[0].InnerHtml
                .Split(new[] { "\r\n", "\r", "\n"}, StringSplitOptions.TrimEntries)
                .Where(line => line.Contains(@"c: ", StringComparison.OrdinalIgnoreCase));

            var list = ExtractMatches(uniqueCStrings, cccamRegExPattern);
            
            return SplitLinesIntoHashSet(list.ToHashSet()).ToList();
        }

        public List<string> ExtractMatches(IEnumerable<string> inputs, string pattern)
        {
            Regex regex = new Regex(pattern);
            List<string> matchList = new List<string>();

            foreach (string input in inputs)
            {
                MatchCollection matches = regex.Matches(input);
                foreach (Match match in matches)
                {
                    matchList.Add(match.Value);
                }
            }

            return matchList;
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
                Match match = Regex.Match(cline, cccamRegExPattern);
                if (!match.Success)
                {
                    return null;
                }

                CcCamLine result = new CcCamLine();
                result.Hostname = match.Groups[1].Value.Trim();
                result.Port = match.Groups[2].Value.Trim();
                result.Username = match.Groups[3].Value.Trim();
                result.Password = match.Groups[4].Value.Trim();

                string versionGroup = match.Groups[5].Value.Trim();
                string[] validVersions = { "2.0.11", "2.1.1", "2.1.2", "2.1.3", "2.1.4", "2.2.8", "2.2.1", "2.3.6", "2.3.1", "2.3.2" };
                foreach (string validVersion in validVersions)
                {
                    if (versionGroup.Contains(validVersion))
                    {
                        result.Cccversion = validVersion;
                        break;
                    }
                }

                return result;
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
            return x.Device.ToLowerInvariant() == y.Device.ToLowerInvariant()
                   && x.User.ToLowerInvariant() == y.User.ToLowerInvariant()
                   && x.Password.ToLowerInvariant() == y.Password.ToLowerInvariant();
        }

        public int GetHashCode(OsCamReader obj)
        {
            // Return a hash code based on the values you're comparing in the Equals method.
            // For example, if you're comparing based on the Label property:
            return obj?.Label?.GetHashCode() ?? 0;
        }
    }
}