using CCCamScraper.Configurations;
using CCCamScraper.QuartzJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Debugging;
using System;
using System.IO;
using System.Threading;

namespace CCCamScraper
{
    public class Program
    {
        private static IConfigurationRoot _configuration;
        
        public static void Main(string[] args)
        {

            var services = new ServiceCollection();

            try
            {
                SelfLog.Enable(Console.Error);

                Thread.CurrentThread.Name = "CCCamScraper main thread";

                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(_configuration)
                    .Enrich.FromLogContext()
                    .CreateLogger();

                if (args.Length > 0)
                    Log.Information("Args: {Args}", args);

                Log.Information("Starting CCCamScraper...");
                var builder = CreateHostBuilder(args).Build();

                Log.Information("Waiting for schedule to start work.");
                builder.Run();
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "CCCamScraper terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .UseSerilog()
    
               .ConfigureServices((hostContext, services) => {
                   //services.AddQuartz(q => {
                   //    q.ScheduleJob<CCCamScraperJob>(CloudWatcherTrigger => CloudWatcherTrigger.WithIdentity("CloudWatcherTrigger", "conditionsTriggers")
                   //    .StartAt(DateTimeOffset.Now.AddSeconds(10))
                   //    .WithSimpleSchedule(x => x
                   //       .WithIntervalInMinutes(1)
                   //       .RepeatForever())
                   //    , meteoJob => meteoJob.WithIdentity("CloudWatcherDatas", "conditions"));
                   //});
                   ////services.AddQuartz(q => {
                   ////    q.ScheduleJob<ServiceWebServer>(ServiceWebServerTrigger => ServiceWebServerTrigger.WithIdentity("ServiceWebServer", "webTriggers")
                   ////    .StartNow()
                   ////    , WebJob => WebJob.WithIdentity("ServiceWebServer", "Web"));
                   ////});
                   
                   services.AddSingleton(_ =>
                   {
                       CCCamScraperOptions scraperOptions = new CCCamScraperOptions();
                       _configuration.GetSection("OsCam").Bind(scraperOptions);

                       return scraperOptions;
                   });

                   services.AddSingleton(_ =>
                   {
                       QuartzJobsOptions quartzJobsOptions = new QuartzJobsOptions();
                       _configuration.GetSection("QuartzJobs").Bind(quartzJobsOptions);

                       return quartzJobsOptions;
                   });

                   services.AddQuartz(q =>
                   {
                       q.UseMicrosoftDependencyInjectionJobFactory();

                       // Register the job, loading the schedule from configuration
                       q.AddJobAndTrigger<TestiousScraperJob>(_configuration);
                   });

                   services.AddQuartz(q =>
                   {
                       q.UseMicrosoftDependencyInjectionJobFactory();

                       // Register the job, loading the schedule from configuration
                       q.AddJobAndTrigger<FourCardSharingScraperJob>(_configuration);
                   });

                   services.AddQuartz(q =>
                   {
                       q.UseMicrosoftDependencyInjectionJobFactory();

                       // Register the job, loading the schedule from configuration
                       q.AddJobAndTrigger<RemoveReadersWithoutUserDefinedCAIDJob>(_configuration);
                   });

                   services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
                   services.AddQuartzServer(options => { options.WaitForJobsToComplete = true; });
               });

    }
}