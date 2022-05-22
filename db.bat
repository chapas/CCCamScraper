docker image rm chapas/cccamscraper:armv7 -f
docker build -t cccamscraper:armv7 .
docker tag cccamscraper:armv7 chapas/cccamscraper:armv7
docker push chapas/cccamscraper:armv7