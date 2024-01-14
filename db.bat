docker image rm chapas/cccamscraper:armv8 -f
docker build -t cccamscraper:armv8 .
docker tag cccamscraper:armv8 chapas/cccamscraper:armv8
docker push chapas/cccamscraper:armv8