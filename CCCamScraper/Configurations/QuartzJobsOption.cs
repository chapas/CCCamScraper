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
        public string URLToScrap { get; set; }
        public string Schedule { get; set; }
    }
}