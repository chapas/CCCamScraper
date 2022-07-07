# Name
CCCamScraper

## Description
CCCamScraper will scrap websites for C lines and parse them to your oscam.server  
Relies on AngleSharp and Quartz
It was only tested as a docker image deployed on docker ver > 20 on RPI4 with OSMC

## Install instructions
 * An image for RPI4 OSMC (debian buster 32bit) exists at https://hub.docker.com/r/chapas/cccamscraper
 * Add a cron job to restart OsCam to reload readers, ie: 51 23 * * * docker restart oscam 
 * oscam.server needs to be on a volume

 ### Run image with
 docker run -d \
--name=cccamscraper \
-e PUID=1000 \
-e PGID=1000 \
-e TZ=Europe/London \
-v "$(pwd)/oscam/config/oscam/:/app/config/" \
--restart unless-stopped \
chapas/cccamscraper:armv7

### Schedule it with 'Cron':
crontab -e
(Paste at the end)
51 23 * * * docker restart oscam
Save and exit
crontab -l (just to make sure)

### Job schedule order
 - Scrape
 - Restart oscam (to reload readers)(not supported by util, done externaly) 
 - Remove undesired CAID's (reads from oscam reder details page)

### Todo
 - Suggestions