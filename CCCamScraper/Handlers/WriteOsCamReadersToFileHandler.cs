using CCCamScraper.Configurations;
using CCCamScraper.Models;
using Microsoft.Extensions.Options;
using Quartz;

namespace CCCamScraper.Handlers
{
    public class WriteOsCamReadersToFileHandler : IHandler
    {
        private readonly IOptionsMonitor<CCCamScraperOptions> _ccCamScraperOptions;
        private IHandler _nextHandler;

        public WriteOsCamReadersToFileHandler(IOptionsMonitor<CCCamScraperOptions> ccCamScraperOptions)
        {
            _ccCamScraperOptions = ccCamScraperOptions;
        }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return _nextHandler;
        }

        public Task<object> Handle(IJobExecutionContext context)
        {
            ScraperJobOperations.WriteOsCamReadersToFile(
                (List<OsCamReader>)context.Result, 
                _ccCamScraperOptions.CurrentValue.OscamServerPath);

            return _nextHandler?.Handle(context)!;
        }
    }
}