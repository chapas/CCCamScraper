using System.ComponentModel.DataAnnotations;

namespace CCCamScraper.Configurations;

public class CcCamScraperJobOption
{
    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string UrlToScrape { get; set; } = default!;

    public string ScrapePath { get; set; } = default!;

    [Required] 
    public string Schedule { get; set; } = default!;

    [Required] 
    public bool RunOnceAtStartUp { get; set; } = false;

    [Required]
    public bool Enabled { get; set; } = true;

    public int? RandomnessInMinutes { get; set; }
}