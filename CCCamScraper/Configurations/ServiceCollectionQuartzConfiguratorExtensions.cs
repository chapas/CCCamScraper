using CCCamScraper.QuartzJobs;
using Microsoft.Extensions.Configuration;
using Quartz;
using System;

namespace CCCamScraper.Configurations
{
    public static class ServiceCollectionQuartzConfiguratorExtensions
    {
        public static void AddQuartzJobsAndTriggers<T>(this IServiceCollectionQuartzConfigurator quartz, IConfigurationRoot serviceProvider) where T : IJob
        {
            var quartzJobsOptions = new QuartzJobsOptions();
            serviceProvider.GetSection("QuartzJobs").Bind(quartzJobsOptions);

            var quartzjobs = quartzJobsOptions.CCCamScraperJobs;

            foreach (var quartzjob in quartzjobs)
            {
                // Use the name of the IJob as the appsettings.json key
                //     string jobName = typeof(T).Name;

                // Try and load the schedule from configuration
                //    var configKey = $"Quartz:{jobName}";
                //    var cronSchedule = config[configKey];
                if (quartzjob == null)
                    throw new Exception($"No Quartz.NET Cron schedule found for job in configuration named {nameof(T)}");

                // register the job as before
                var jobKey = new JobKey(quartzjob.Name);

                var type = Type.GetType("CCCamScraper.QuartzJobs." + quartzjob.Name);

                if (type == null) //then it's a scrape job
                    quartz.AddJob(typeof(ScrapeJob), jobKey, opts => opts.WithIdentity(quartzjob.Name));
                else
                    quartz.AddJob(type, jobKey, opts => opts.WithIdentity(quartzjob.Name));

                if (quartzjob.RunOnceAtStartUp)
                    quartz.AddTrigger(opts => opts.ForJob(jobKey)
                        .WithIdentity(quartzjob.Name + "-trigger-now")
                        .StartNow());

                quartz.AddTrigger(opts => opts.ForJob(jobKey)
                    .WithIdentity(quartzjob.Name + "-trigger")
                    .WithCronSchedule(quartzjob.Schedule));
            }
        }
    }
}