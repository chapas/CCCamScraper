using System.ComponentModel.DataAnnotations;

namespace CCCamScraper.Configurations
{
    /// <summary>
    /// Maintainer configurations
    /// </summary>
    public class CCCamScraperOptions
    {
        /// <summary>
        /// Gets or sets the server path
        /// </summary>
        /// <value>
        /// The oscam.server file path
        /// </value>
        [Required]
        public string OscamServerPath { get; set; }

        /// <summary>
        /// Gets or sets the number of backups
        /// </summary>
        /// <value>
        /// Number of backups to keep of oscam.server
        /// </value>
        public int NumberOfBackupsToKeep { get; set; }

        /// <summary>
        /// Gets or sets OsCam status endpoint
        /// </summary>
        /// <value>
        /// The URL with status page from OsCam
        /// </value>
        public string OsCamStatusPageUrl { get; set; }

        /// <summary>
        /// Gets or sets OsCam reader endpoint
        /// </summary>
        /// <value>
        /// The URL with reader entitlements (available shares)
        /// </value>
        public string OsCamReaderPageUrl { get; set; }

        /// <summary>
        /// Gets or sets OsCam reader endpoint
        /// </summary>
        /// <value>
        /// The URL with reader entitlements (available shares)
        /// </value>
        public string OsCamReaderApiurl { get; set; }

        /// <summary>
        /// Gets or sets allowed CAID's
        /// </summary>
        /// <value>
        /// A list of CAID's that we find on the readers
        /// </value>
        public string[] CaiDs { get; set; }

        /// <summary>
        /// Gets or sets servers excluded from being deleted
        /// </summary>
        /// <value>
        /// A list of servers excluded we don't want to delete
        /// </value>
        public string[] ExcludedFromDeletion { get; set; }

        /// <summary>
        /// Gets or sets the status of servers that will be deleted
        /// </summary>
        /// <value>
        /// A list of servers status we will delete
        /// </value>
        public string[] UnwantedStatus { get; set; }
    }
}