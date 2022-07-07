using CCCamScraper.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CCCamScraper.QuartzJobs
{
    public class ScrapeJob : IJob
    {
        internal static ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        internal CCCamScraperJobOption quartzJobsOption;

        public ScrapeJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await CheckCCCamServerstate(context);
        }

        public async Task CheckCCCamServerstate(IJobExecutionContext context)
        {
            quartzJobsOption = _serviceProvider.GetRequiredService<QuartzJobsOptions>().CCCamScraperJobs.FirstOrDefault(qjob => qjob.Name == context.JobDetail.Key.Name) ?? throw new InvalidOperationException();
            var cccamScraperOptions = _serviceProvider.GetRequiredService<CCCamScraperOptions>();
            try
            {
                if (quartzJobsOption != null)
                {
                    var scrapedCLinesFromUrl = await ScraperJobOperations.ScrapeCLinesFromUrl(quartzJobsOption)
                        .ConfigureAwait(false);

                    var parsedCLines =
                        ScraperJobOperations.ParseCLines(scrapedCLinesFromUrl, quartzJobsOption.URLToScrape);

                    var readersFromOscamServer = await ScraperJobOperations
                        .GetListWithCurrentReadersOnOscamServerFile(cccamScraperOptions.OscamServerPath)
                        .ConfigureAwait(false);

                    var currentListOfCcCamReadersFromFileNew =
                        ScraperJobOperations.AddNewScrapedReaders(readersFromOscamServer, parsedCLines);

                    ScraperJobOperations.WriteOsCamReadersToFile(currentListOfCcCamReadersFromFileNew,
                        cccamScraperOptions.OscamServerPath);
                }
                else
                {
                    _logger.Error($"Couldn't find a Quartz job named: {GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
        }
    }
}