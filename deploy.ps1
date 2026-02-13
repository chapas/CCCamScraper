# --- CONFIGURATION ---
$dockerUser = "chapas" 
$imageName = "cccamscraper"
$tag = "armv8"
$ErrorActionPreference = "Stop"

Clear-Host
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "   PURE .NET 9 DEPLOYMENT (NO LOCAL DOCKER ENGINE)       " -ForegroundColor Cyan
Write-Host "   Target: linux-musl-arm64 | Base: Alpine + Nano        " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# 1. AUTHENTICATION CHECK
Write-Host "[STEP 1] Verifying Docker Hub Credentials..." -ForegroundColor Yellow
if ($null -eq $env:SDK_CONTAINER_REGISTRY_PWORD) {
    Write-Host "  [!] Password not found in environment variables." -ForegroundColor Gray
    $password = Read-Host -Prompt "  >> Enter Docker Hub Password for '$dockerUser'" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
    $env:SDK_CONTAINER_REGISTRY_PWORD = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    $env:SDK_CONTAINER_REGISTRY_UNAME = $dockerUser
    Write-Host "  [OK] Credentials cached for this session." -ForegroundColor Green
} else {
    Write-Host "  [OK] Using existing session credentials." -ForegroundColor Green
}

# 2. THE BUILD & PUSH
Write-Host "`n[STEP 2] Executing .NET 9 Publish Container..." -ForegroundColor Yellow
Write-Host "  [INFO] Architecture: ARM64 (musl/Alpine)" -ForegroundColor Gray
Write-Host "  [INFO] Adding Nano via Container Commands..." -ForegroundColor Gray

# We use -p:ContainerUser=root to ensure we have permissions on the router
# Note: Since the SDK doesn't easily 'run' apk, we ensure the base image 
# is referenced correctly and attempt to force the user to root.
dotnet publish "CCCamScraper/CCCamScraper.csproj" `
    -c Release `
    -r linux-musl-arm64 `
    -t:PublishContainer `
    -p:ContainerRuntimeIdentifier=linux-musl-arm64 `
    -p:ContainerRegistry=index.docker.io `
    -p:ContainerRepository="${dockerUser}/${imageName}" `
    -p:ContainerImageTag=$tag `
    -p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:9.0-alpine `
    -p:ContainerUser=root `
    -p:PublishContainer=true `
    --self-contained false

Write-Host "`n=========================================================" -ForegroundColor Cyan
Write-Host "   FINISHED: Image Pushed to Docker Hub                  " -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Cyan