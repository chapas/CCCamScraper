# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet publish "CCCamScraper/CCCamScraper.csproj" -c Release -r linux-musl-arm64 -o /app --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

# INSTALL NANO
RUN apk update && apk add --no-cache nano

COPY --from=build /app .

# Ensure we run as root so nano can edit mapped volumes
USER root
ENTRYPOINT ["dotnet", "CCCamScraper.dll"]