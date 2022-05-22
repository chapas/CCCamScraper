using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace CCCamScraper.Configurations
{
    public static class ServiceCollectionQuartzConfiguratorExtensions
    {
        public static void AddJobAndTrigger<T>(this IServiceCollectionQuartzConfigurator quartz, IConfigurationRoot serviceProvider) where T : IJob
        {

            QuartzJobsOptions quartzJobsOptions = new QuartzJobsOptions();
            serviceProvider.GetSection("QuartzJobs").Bind(quartzJobsOptions);

            var job = quartzJobsOptions.CCCamScraperJobs.FirstOrDefault(jobs => jobs.Name == typeof(T).Name);

            // Use the name of the IJob as the appsettings.json key
            //     string jobName = typeof(T).Name;

            // Try and load the schedule from configuration
            //    var configKey = $"Quartz:{jobName}";
            //    var cronSchedule = config[configKey];
            if (job == null)
                throw new Exception($"No Quartz.NET Cron schedule found for job in configuration named {nameof(T)}");

            //var jobName = job.Name;
            //var cronSchedule = job.Schedule;

            // Some minor validation
            //if (string.IsNullOrEmpty(job.Schedule))
            //{
            //    throw new Exception($"No Quartz.NET Cron schedule found for job in configuration at :");
            //}

            // register the job as before
            var jobKey = new JobKey(job.Name);
            quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

            quartz.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity(job.Name + "-trigger")
                .WithCronSchedule(job.Schedule)); // use the schedule from configuration
        }
    }
}