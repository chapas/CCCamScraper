namespace CCCamScraper;

using CCCamScraper.Configurations;
using CCCamScraper.QuartzJobs.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Debugging;

public class Program
{
    private static IConfigurationRoot _configuration;

    public static void Main(string[] args)
    {
        try
        {
            SelfLog.Enable(Console.Error);
            Thread.CurrentThread.Name = "CCCamScraper main thread";

            _configuration = new ConfigurationBuilder()
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
            var host = CreateHostBuilder(args).Build();

            Log.Information("Service initialized. Waiting for schedule to start work.");
            host.Run();
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
                // 1. Setup Options
                services.AddOptions<CCCamScraperOptions>()
                    .BindConfiguration("OsCam")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddOptions<QuartzJobsOptions>()
                    .BindConfiguration("QuartzJobs")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                // 2. Quartz Configuration
                services.AddQuartz(q =>
                {
                    var quartzOptions = _configuration.GetSection("QuartzJobs").Get<QuartzJobsOptions>();

                    if (quartzOptions?.CcCamScraperJobs != null)
                    {
                        var enabledJobs = quartzOptions.CcCamScraperJobs.Where(j => j.Enabled).ToList();

                        foreach (var jobOption in enabledJobs)
                        {
                            var jobKey = new JobKey(jobOption.Name);
                            Type? jobType = null;

                            // 1. Try to find a specific class matching the Name (e.g., RemoveReadersWithECMNotOKJob)
                            jobType = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.Name.Equals(jobOption.Name, StringComparison.OrdinalIgnoreCase)
                                                     && typeof(IJob).IsAssignableFrom(t));

                            if (jobType == null)
                            {
                                Log.Information("No specific class found for {JobName}, defaulting to ScrapeJob.", jobOption.Name);
                                jobType = typeof(ScrapeJob);
                            }
                            else
                            {
                                Log.Information("Specific job class found: {JobType}", jobType.Name);
                            }

                            q.AddJob(jobType, jobKey, (Action<IJobConfigurator>?)null);

                            q.AddTrigger(opts =>
                            {
                                opts.ForJob(jobKey).WithIdentity(jobOption.Name + "-cron-trigger");

                                if (int.TryParse(jobOption.Schedule, out int minutes))
                                {
                                    opts.WithSimpleSchedule(x => x.WithIntervalInMinutes(minutes).RepeatForever());
                                }
                                else
                                {
                                    opts.WithCronSchedule(jobOption.Schedule);
                                }
                            });

                            if (jobOption.RunOnceAtStartUp)
                            {
                                q.AddTrigger(opts => opts
                                    .ForJob(jobKey)
                                    .WithIdentity(jobOption.Name + "-startup-trigger")
                                    .StartNow());
                            }
                        }
                    }
                });

                services.AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });
            });
    }
}