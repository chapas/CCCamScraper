using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CCCamScraper.QuartzJobs
{
    public abstract class ScraperJobBase
    {
        protected readonly IServiceProvider _serviceProvider;
        internal static ILogger _logger;

        public ScraperJobBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task CheckCCCamServerstate()
        {
            var quartzJobsOption = _serviceProvider.GetRequiredService<QuartzJobsOptions>().CCCamScraperJobs.FirstOrDefault(qjob => qjob.Name == GetType().Name);
            var cccamScraperOptions = _serviceProvider.GetRequiredService<CCCamScraperOptions>();
            try
            {
                if (quartzJobsOption != null)
                {
                    var scrapedCLinesFromUrl = await ScrapeCLinesFromUrl(quartzJobsOption.URLToScrap).ConfigureAwait(false);
                    
                    var parsedCLines = ScraperJobOperations.ParseCLines(scrapedCLinesFromUrl, quartzJobsOption.URLToScrap);
                    
                    var readersFromOscamServer = await ScraperJobOperations.GetListWithCurrentReadersOnOscamServerFile(cccamScraperOptions.OscamServerPath).ConfigureAwait(false);

                    List<OsCamReader> currentListOfCcCamReadersFromFileNew = ScraperJobOperations.AddNewScrapedReaders(readersFromOscamServer, parsedCLines);
                      
                    ScraperJobOperations.WriteOsCamReadersToFile(currentListOfCcCamReadersFromFileNew, cccamScraperOptions.OscamServerPath);
                }
                else
                    _logger.Error($"Couldn't find a Quartz job named: {GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
        }

        public abstract Task<List<string>> ScrapeCLinesFromUrl(string urlToScrapeFrom);

        internal static string UrlStringReplacement(string url)
        {
            if (!(url.Contains('<') & url.Contains('>')))
                return url;

            string _day = DateTime.Today.Day.ToString("00", CultureInfo.InvariantCulture);
            string _month = DateTime.Today.Month.ToString("00", CultureInfo.InvariantCulture);
            string _year = DateTime.Today.Year.ToString("0000", CultureInfo.InvariantCulture);

            url = url.Replace("<yyyy>", _year);
            url = url.Replace("<mm>", _month);
            url = url.Replace("<dd>", _day);

            return url;
        }
    }
}