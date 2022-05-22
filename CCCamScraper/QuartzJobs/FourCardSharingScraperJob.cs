using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Io;
using Serilog;

namespace CCCamScraper.QuartzJobs
{
    [DisallowConcurrentExecution]
    public class FourCardSharingScraperJob : ScraperJobBase, IJob
    {
        public FourCardSharingScraperJob(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task Execute(IJobExecutionContext context) => await CheckCCCamServerstate().ConfigureAwait(false);

        public override async Task<List<string>> ScrapeCLinesFromUrl(string urlToScrapeFrom)
        {
            Log.Information($"Started scraping on {urlToScrapeFrom}");

            //We need to add the browser headers
            DefaultHttpRequester req = new DefaultHttpRequester();
            req.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0";

            // Load default configuration
            var config = Configuration.Default.With(req).WithDefaultLoader().WithDefaultCookies();  // Create a new browsing context
            var context = BrowsingContext.New(config);                                                          // This is where the HTTP request happens, returns <IDocument> that // we can query later
            var document = await context.OpenAsync(urlToScrapeFrom);                                            // Log the data to the console

            //firewall blocking this will yield ZERO lines (damn)
            var lines = document.QuerySelectorAll("div div section")
                .Select(m => m.InnerHtml.Replace("<br>", "").Replace("</p>", "")
                    .Trim().Split("\n"));

            var cLines = lines.ToList()[0].Where(line => line.ToLower().Trim().StartsWith("c:")).ToList();

            if (cLines.Any())
            {
                Log.Information($"Scraped {cLines.Count()} C lines from {urlToScrapeFrom}");
                return cLines;
            }

            Log.Warning($"Scraped ZERO C lines from {urlToScrapeFrom}");
            return new List<string>();
        }
    }
}