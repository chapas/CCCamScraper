#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0.1-bookworm-slim-arm64v8 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.101 AS build
WORKDIR /src
COPY ["CCCamScraper.Models/CCCamScraper.Models.csproj", "CCCamScraper.Models/"]
RUN dotnet restore "CCCamScraper.Models/CCCamScraper.Models.csproj"
COPY ["CCCamScraper/CCCamScraper.csproj", "CCCamScraper/"]
RUN dotnet restore "CCCamScraper/CCCamScraper.csproj"
COPY . .
WORKDIR "/src/CCCamScraper.Models"
RUN dotnet build "CCCamScraper.Models.csproj" -r linux-arm64 -c Release -o /app/build
WORKDIR "/src/CCCamScraper"
RUN dotnet build "CCCamScraper.csproj" -r linux-arm64 -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "/src/CCCamScraper.Models/CCCamScraper.Models.csproj" -r linux-arm64 -c Release -o /app/publish
RUN dotnet publish "/src/CCCamScraper/CCCamScraper.csproj" -r linux-arm64 -c Release -o /app/publish

FROM base AS final
WORKDIR /app
VOLUME /oscam
COPY --from=publish /app/publish .
RUN apt-get update && apt-get upgrade -y
RUN apt-get install nano -y
ENTRYPOINT ["dotnet", "CCCamScraper.dll"]