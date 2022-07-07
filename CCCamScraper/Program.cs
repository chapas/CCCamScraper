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
                    .AddJsonFile("appsettings.json", false, true)
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(_ =>
                    {
                        var scraperOptions = new CCCamScraperOptions();
                        _configuration.GetSection("OsCam").Bind(scraperOptions);

                        return scraperOptions;
                    });
                    services.AddSingleton(_ =>
                    {
                        var quartzJobsOptions = new QuartzJobsOptions();
                        _configuration.GetSection("QuartzJobs").Bind(quartzJobsOptions);

                        return quartzJobsOptions;
                    });
                    services.AddQuartz(q =>
                    {
                        q.UseMicrosoftDependencyInjectionJobFactory();

                        // Register the job, loading the schedule from configuration
                        q.AddQuartzJobsAndTriggers<ScrapeJob>(_configuration);
                    });

                    services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
                    services.AddQuartzServer(options => { options.WaitForJobsToComplete = true; });
                });
        }
    }
}