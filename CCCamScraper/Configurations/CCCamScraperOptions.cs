using System.ComponentModel.DataAnnotations;

namespace CCCamScraper.Configurations;

/// <summary>
/// Maintainer configurations
/// </summary>
public class CCCamScraperOptions
{
    /// <summary>
    /// Gets or sets the server path
    /// </summary>
    [Required]
    public string OscamServerPath { get; set; } = default!;

    /// <summary>
    /// Gets or sets the number of backups
    /// </summary>
    public int NumberOfBackupsToKeep { get; set; }

    /// <summary>
    /// Gets or sets OsCam status endpoint
    /// </summary>
    public string OsCamStatusPageUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets OsCam entitlements endpoint
    /// </summary>
    public string OsCamEntitlementsPageUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets OsCam readers endpoint
    /// </summary>
    public string OsCamReadersPageUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets OsCam reader endpoint
    /// </summary>
    public string OsCamReaderApiurl { get; set; } = default!;

    /// <summary>
    /// Gets or sets allowed CAID's
    /// </summary>
    public string[] CaiDs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets servers excluded from being deleted
    /// </summary>
    public string[] ExcludedFromDeletion { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the status of servers that will be deleted
    /// </summary>
    public string[] UnwantedStatus { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the FlareSolverr URL for bypassing Cloudflare protections
    /// </summary>
    public string FlareSolverrUrl { get; set; } = @"http://192.168.1.1:8191/v1";

    /// <summary>
    /// Gets or sets the threshold for ECM OK readers, if the number of ECM OK readers is above this threshold, the scraper will not delete any readers
    /// </summary>
    /// <remarks>Not really a threshold but the minimum fixed value to look for.
    public int EcmOkThreshold { get; set; } = 0;

    /// <summary>
    /// Gets or sets the threshold for ECM NOK readers, if the number of ECM NOK readers is above this threshold, the scraper will not delete any readers
    /// </summary>
    /// <remarks>This is minimum a threshold, greater numbers will mean a deleted reader.
    public int EcmNokThreshold { get; set; } = 20;
}