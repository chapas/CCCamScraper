using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CCCamScraper.Configurations
{
    /// <summary>
    /// Implementation of the configurable oscam maintiainer options
    /// </summary>
    public class ConfigureMaintainerOptions : IConfigureOptions<CCCamScraperOptions>
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Instantiates a <see cref="ConfigureMaintainerOptions"/>
        /// </summary>
        /// <param name="configuration">Service configuration</param>
        public ConfigureMaintainerOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // <inherit/>
        public void Configure(CCCamScraperOptions options)
        {
            _configuration.GetSection("OsCam").Bind(options);
        }
    }
}
