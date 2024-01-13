using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz;

namespace CCCamScraper.QuartzJobs
{
    public class ParseCLinesHandler : IHandler
    {
        private IHandler _nextHandler;

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return _nextHandler;
        }

        public Task<object> Handle(object request, IJobExecutionContext context)
        {
            var result = ScraperJobOperations.ParseCLines((List<string>)request, context.JobDetail.Key.Name);
            return _nextHandler?.Handle(result, context);
        }
    }
}