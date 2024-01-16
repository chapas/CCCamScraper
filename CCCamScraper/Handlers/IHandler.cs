using Quartz;

namespace CCCamScraper.Handlers;

public interface IHandler
{
    IHandler SetNext(IHandler handler);

    Task<object> Handle(IJobExecutionContext context);
}