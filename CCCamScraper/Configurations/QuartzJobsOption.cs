using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        public string UrlToScrape { get; set; }
        public string ScrapePath { get; set; }
        public string Schedule { get; set; }
        public bool RunOnceAtStartUp { get; set; } = false;
    }
}