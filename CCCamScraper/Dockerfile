#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.101 AS build
WORKDIR /src
COPY ["CCCamScraper/CCCamScraper.csproj", "CCCamScraper/"]
#COPY ["CCCamScraper.Models/CCCamScraper.Models.csproj", "CCCamScraper.Models/"]
RUN dotnet restore "/src/CCCamScraper/CCCamScraper.sln"
COPY . .
WORKDIR "/src/CCCamScraper"
#RUN dotnet build "src/CCCamScraper.Models/CCCamScraper.Models.csproj" --runtime amd64 -c Release -o /app/build
RUN dotnet build "src/CCCamScraper/CCCamScraper.csproj" --runtime amd64 -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CCCamScraper.sln" --runtime amd64 -c Release -o /app/publish

FROM base AS final
WORKDIR /app
VOLUME /oscam
COPY --from=publish /app/publish .
#RUN apt-get update && apt-get update -y
#RUN apt-get install nano -y
ENTRYPOINT ["dotnet", "CCCamScraper.dll"]