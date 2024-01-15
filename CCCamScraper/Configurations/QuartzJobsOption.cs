using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CCCamScraper.Configurations
{
    public class QuartzJobsOptions
    {
        public List<CcCamScraperJobOption> CcCamScraperJobs { get; set; }
    }

    public class CcCamScraperJobOption
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string UrlToScrape { get; set; }

        public string ScrapePath { get; set; }
        [Required] 
        public string Schedule { get; set; }
        [Required] 
        public bool RunOnceAtStartUp { get; set; } = false;
    }
}