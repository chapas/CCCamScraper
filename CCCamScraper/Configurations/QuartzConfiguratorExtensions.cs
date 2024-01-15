using Microsoft.Extensions.Configuration;
using Quartz;
using CCCamScraper.QuartzJobs.Jobs;
using Microsoft.Extensions.Options;

namespace CCCamScraper.Configurations
{
    public static class QuartzConfiguratorExtensions
    {
        public static void AddQuartzJobsAndTriggers(
            this IServiceCollectionQuartzConfigurator quartz,
            IConfigurationRoot configuration,
            IOptionsMonitor<QuartzJobsOptions> quartzJobsOptions)
        {
            foreach (var quartzJob in quartzJobsOptions.CurrentValue.CcCamScraperJobs)
            {
                if (quartzJob == null)
                    throw new Exception($"No Quartz.NET Cron schedule found for job in configuration named {quartzJob.Name}");

                var jobKey = new JobKey(quartzJob.Name);
                var jobType = Type.GetType($"CCCamScraper.QuartzJobs.Jobs.{quartzJob.Name}") ?? typeof(ScrapeJob);

                quartz.AddJob(jobType, jobKey, opts => opts.WithIdentity(quartzJob.Name));

                if (quartzJob.RunOnceAtStartUp)
                    quartz.AddTrigger(opts => opts.ForJob(jobKey)
                        .WithIdentity($"{quartzJob.Name}-trigger-now")
                        .StartNow());

                quartz.AddTrigger(opts => opts.ForJob(jobKey)
                    .WithIdentity($"{quartzJob.Name}-trigger")
                    .WithCronSchedule(quartzJob.Schedule));
            }
        }
    }
}