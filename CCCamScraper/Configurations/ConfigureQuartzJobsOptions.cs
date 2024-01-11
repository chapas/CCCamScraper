using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

//using ILogger = Serilog.ILogger;

namespace CCCamScraper.Configurations
{
    public class ConfigureQuartzJobsOptions : IConfigureOptions<QuartzJobsOptions>
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Instantiates a <see cref="QuartzJobsOptions" />
        /// </summary>
        /// <param name="configuration">Service configuration</param>
        public ConfigureQuartzJobsOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // <inherit/>
        public void Configure(QuartzJobsOptions options)
        {
            _configuration.GetSection("Quartz").Bind(options);
        }
    }
}