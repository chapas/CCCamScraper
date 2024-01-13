using CCCamScraper.Configurations;
using Microsoft.Extensions.Options;
using Quartz;
using System.Threading.Tasks;

namespace CCCamScraper.Handlers
{
    public class GetCurrentReadersOnOscamServerFileHandler : IHandler
    {
        private readonly IOptionsMonitor<CCCamScraperOptions> _cccamScraperOptions;
        private IHandler _nextHandler;

        public GetCurrentReadersOnOscamServerFileHandler(IOptionsMonitor<CCCamScraperOptions> cccamScraperOptions)
        {
            _cccamScraperOptions = cccamScraperOptions;
        }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return _nextHandler;
        }

        public async Task<object> Handle(IJobExecutionContext context)
        {
            context.Result = await ScraperJobOperations.GetListWithCurrentReadersOnOsCamServerFile(_cccamScraperOptions.CurrentValue.OscamServerPath).ConfigureAwait(false);

            return _nextHandler.Handle(context);
        }
    }
}