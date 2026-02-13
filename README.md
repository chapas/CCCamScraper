# 🛰️ CCCamScraper

## Description

**CCCamScraper** is a high-performance .**NET** 9 utility designed to automate the lifecycle of OSCam readers. It scrapes C-Lines from specified websites, parses them into a valid `oscam.server` configuration, and performs automated maintenance by analyzing the OSCam WebUI to remove non-functional or low-performance readers.

The application is built on **AngleSharp** for robust **HTML** parsing and **Quartz.**NET**** for complex job scheduling.

---

## 🚀 Key Features

- **Cloudflare Bypass**: Integrated support for [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr) to scrape sites protected by Cloudflare/DDoS-Guard
- **Dynamic Scraping**: Scrape C-Lines from any forum or website using **CSS** selectors
- **Date-Aware URLs**: Supports `<**YYYY**>`, `<MM>`, and `<DD>` placeholders in URLs (**case-insensitive**) for scraping daily forum threads
- **Automated Maintenance**:
    - ****ECM** Health Check**: Automatically removes readers based on performance thresholds (e.g. 0 OK and > 20 **NOK**)
    - **Status Filtering**: Removes readers marked as `**ERROR**`, `**OFF**`, or `**UNKNOWN**`
    - ****CAID** Filtering**: Ensures only readers supporting your defined CAIDs are kept
- **Reflection-Based Job Loading**: Automatically maps `appsettings.json` job names to C# Job classes
- **Intelligent Fallback**: If a job name doesn't match a specific maintenance class, it defaults to a standard `ScrapeJob`
- **Docker Ready**: Optimized for **ARM64** (FriendlyWRT/NanoPi **R6C**) and standard Linux environments

---

## 🛠️ Configuration (`appsettings.json`)

### 1. OsCam & FlareSolverr Settings

Configure your server paths, maintenance thresholds, and FlareSolverr **API** address.

```json
*OsCam*: {
    *FlareSolverrUrl*: *[http://**192**.**168**.1.1:**8191**/v1*,](http://**192**.**168**.1.1:**8191**/v1*,)
    *OscamServerPath*: *config/oscam.server*,
    *OsCamReadersPageURL*: *[http://**192**.**168**.1.1:**8888**/readers.html*,](http://**192**.**168**.1.1:**8888**/readers.html*,)
    *EcmOkThreshold*: 0,
    *EcmNokThreshold*: 20,
    *CAIDs*: [ *1814*, *1802* ],
    *UnwantedStatus*: [ *ERROR*, *OFF*, *UNKNOWN* ],
    *ExcludedFromDeletion*: [ *premium-server.net*, *myserver.io* ]
}
## Quartz Job Settings
The Name field is used via reflection to automatically find and execute the matching C# job class.
To use a specialized job (e.g. maintenance task), the name in the **JSON** must exactly match the C# class name (case-sensitive).
**JSON***QuartzJobs*: {
    *CCCamScraperJobs*: [
    {
    *Name*: *RemoveReadersWithECMNotOKJob*,
    *Schedule*: *0 0 */4 * * ?*,
    *RunOnceAtStartUp*: true,
    *Enabled*: true
    },
    {
    *Name*: *RemoveReadersWithUnwantedStatusJob*,
    *Schedule*: *0 0 */4 * * ?*,
    *Enabled*: false
    },
    {
    *Name*: *freeclinePrint*,
    *URLToScrape*: *[https://example.com/viewtopic.php?t=91&view=print*,](https://example.com/viewtopic.php?t=91&view=print*,)
    *ScrapePath*: *div.entry div*,
    *Schedule*: *0 0 */4 * * ?*,
    *Enabled": true
    }
    ]
}

📦 Installation & Deployment
### Docker Setup
The application requires a volume mapping to your OSCam configuration directory so it can read/write the oscam.server file.
Bashservices:
  cccam-scraper:
    container_name: cccam-scrapper
    image: chapas/cccamscraper:armv8
    # mandatory for .NET 9 to write to /root volumes
    user: "root" 
    environment:
      - PUID=0
      - PGID=0
      - TZ=Europe/Lisbon
      - CCCamScraperOptions__FlareSolverrUrl=http://172.17.0.1:8191/v1
    restart: unless-stopped
    volumes:
      - /root/oscam/config/oscam:/app/config/oscam
    depends_on:
      - flaresolverr

  flaresolverr:
    image: ghcr.io/flaresolverr/flaresolverr:latest
    container_name: flaresolverr
    environment:
      - LOG_LEVEL=info
      - PUID=0
      - PGID=0
      - TZ=Europe/Lisbon
    ports:
      - "8191:8191"
    restart: unless-stopped
    network_mode: bridge
OSCam Reloading
OSCam must reload the configuration (or be restarted) after oscam.server is modified.
Example via Crontab:
Bash# Restart OSCam container every 4 hours to apply changes
0 */4 * * * docker restart oscam

🧬 Job Execution & Maintenance Rules Reflection-Based Loading At startup, the application scans the assembly for job classes whose name exactly matches the Name value from appsettings.json.

If a matching class is found (e.g. RemoveReadersWithECMNotOKJob) → that specialized logic runs If no match is found → falls back to generic scraping behavior (ScrapeJob)

**ECM** Health Maintenance (RemoveReadersWithECMNotOKJob) This job keeps your reader list clean by:

Loading the OSCam readers.html status page Parsing the dataTable (works with modern OSCam WebUI themes)
Identifying readers where: OK <= EcmOkThresholdANDNOK > EcmNokThreshold Matching reader names using case-insensitiveStartsWith Removing matching readers from oscam.server Saving the cleaned file

📝 Developer Notes

Case Insensitivity: <**YYYY**>, <MM>, <DD> placeholders and reader label matching are fully case-insensitive
Error Handling: Chain of Responsibility pattern + safe fallback on final handler
Logging: Serilog → CCCamScraper.log (rolling daily files)

Disclaimer This utility is provided for testing and educational purposes only. Users are solely responsible for complying with the terms of service of any websites being scraped.