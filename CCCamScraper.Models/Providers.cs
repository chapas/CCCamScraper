namespace CCCamScraper.Models;

/// <summary>
/// Represents a provider with CAID, ID, and Uphops information.
/// </summary>
public class Providers
{
    /// <summary>
    /// Gets or sets the CAID (Conditional Access ID) for the provider.
    /// </summary>
    public string Caid { get; set; }

    /// <summary>
    /// Gets or sets the ID for the provider.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the Uphops (number of hops) for the provider.
    /// </summary>
    public string Uphops { get; set; }
}