#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0.16-buster-slim-arm32v7 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0.407 AS build
WORKDIR /src
COPY ["CCCamScraper/CCCamScraper.csproj", "CCCamScraper/"]
RUN dotnet restore "CCCamScraper/CCCamScraper.csproj"
COPY . .
WORKDIR "/src/CCCamScraper"
RUN dotnet build "CCCamScraper.csproj" -r linux-arm -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CCCamScraper.csproj" -r linux-arm -c Release -o /app/publish

FROM base AS final
WORKDIR /app
VOLUME /oscam
COPY --from=publish /app/publish .
RUN apt-get update && apt-get update -y
RUN apt-get install nano -y
ENTRYPOINT ["dotnet", "CCCamScraper.dll"]