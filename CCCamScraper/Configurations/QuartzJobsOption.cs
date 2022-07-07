using System.Collections.Generic;

namespace CCCamScraper.Configurations
{
    public class QuartzJobsOptions
    {
        public List<CCCamScraperJobOption> CCCamScraperJobs { get; set; }
    }

    public class CCCamScraperJobOption
    {
        public string Name { get; set; }
        public string URLToScrape { get; set; }
        public string ScrapePath { get; set; }
        public string Schedule { get; set; }
        public bool RunOnceAtStartUp { get; set; } = false;
    }
}