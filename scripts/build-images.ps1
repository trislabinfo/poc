# Build Datarizen container images for minikube
# Usage: 
#   .\scripts\build-images.ps1 -Topology Monolith
#   .\scripts\build-images.ps1 -Topology MultiApp
#   .\scripts\build-images.ps1 -Topology Microservices
#   .\scripts\build-images.ps1  # Will prompt for topology selection
# Prerequisites: 
#   - Docker must be running
#   - For minikube: Run `minikube docker-env | Invoke-Expression` first

param(
    [string]$Topology = "",
    [string]$Tag = "local",
    [string]$Registry = "",
    [switch]$Push = $false
)

$ErrorActionPreference = "Stop"

# Get the repository root directory
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

# Check Docker connectivity
Write-Host "Checking Docker connectivity..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "OK Docker is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker is not running or not accessible" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Start Docker Desktop" -ForegroundColor White
    Write-Host "2. Wait for Docker Desktop to fully start (whale icon in system tray)" -ForegroundColor White
    Write-Host "3. Verify Docker is running: docker ps" -ForegroundColor White
    Write-Host "4. If using minikube, run: minikube docker-env | Invoke-Expression" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Define all available images with their topology assignments
$allImages = @(
    @{
        Name = "monolith"
        Dockerfile = "server/src/Hosts/MonolithHost/Dockerfile"
        ImageName = "datarizen-monolith"
        Topology = "Monolith"
    },
    @{
        Name = "controlpanel"
        Dockerfile = "server/src/Hosts/MultiAppControlPanelHost/Dockerfile"
        ImageName = "datarizen-controlpanel"
        Topology = "MultiApp"
    },
    @{
        Name = "runtime"
        Dockerfile = "server/src/Hosts/MultiAppRuntimeHost/Dockerfile"
        ImageName = "datarizen-runtime"
        Topology = "MultiApp"
    },
    @{
        Name = "appbuilder"
        Dockerfile = "server/src/Hosts/MultiAppAppBuilderHost/Dockerfile"
        ImageName = "datarizen-appbuilder"
        Topology = "MultiApp"
    },
    @{
        Name = "identity"
        Dockerfile = "server/src/Hosts/IdentityServiceHost/Dockerfile"
        ImageName = "datarizen-identity"
        Topology = "Microservices"
    },
    @{
        Name = "tenant"
        Dockerfile = "server/src/Hosts/TenantServiceHost/Dockerfile"
        ImageName = "datarizen-tenant"
        Topology = "Microservices"
    },
    @{
        Name = "appbuilder-svc"
        Dockerfile = "server/src/Hosts/AppBuilderServiceHost/Dockerfile"
        ImageName = "datarizen-appbuilder-svc"
        Topology = "Microservices"
    },
    @{
        Name = "tenantapp"
        Dockerfile = "server/src/Hosts/TenantApplicationServiceHost/Dockerfile"
        ImageName = "datarizen-tenantapp"
        Topology = "Microservices"
    },
    @{
        Name = "gateway"
        Dockerfile = "server/src/ApiGateway/Dockerfile"
        ImageName = "datarizen-gateway"
        Topology = "Microservices"
    }
)

# If topology not provided, prompt user to select
if ([string]::IsNullOrWhiteSpace($Topology)) {
    Write-Host ""
    Write-Host "Select topology to build:" -ForegroundColor Yellow
    Write-Host "  1) Monolith - Single application with all modules" -ForegroundColor Cyan
    Write-Host "  2) MultiApp - Multiple applications (Control Panel, Runtime, App Builder)" -ForegroundColor Cyan
    Write-Host "  3) Microservices - Individual services (Identity, Tenant, AppBuilder, TenantApp, Gateway)" -ForegroundColor Cyan
    Write-Host ""
    
    $selection = Read-Host "Enter selection (1-3)"
    
    switch ($selection) {
        "1" { $Topology = "Monolith" }
        "2" { $Topology = "MultiApp" }
        "3" { $Topology = "Microservices" }
        default {
            Write-Host "Invalid selection. Exiting." -ForegroundColor Red
            exit 1
        }
    }
}

# Validate topology
$validTopologies = @("Monolith", "MultiApp", "Microservices")
if ($Topology -notin $validTopologies) {
    Write-Host "ERROR: Invalid topology '$Topology'. Valid options: $($validTopologies -join ', ')" -ForegroundColor Red
    exit 1
}

# Filter images by topology
$images = @($allImages | Where-Object { $_.Topology -eq $Topology })

if ($images.Count -eq 0) {
    Write-Host "ERROR: No images found for topology '$Topology'" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Building Datarizen container images for topology: $Topology" -ForegroundColor Green
Write-Host "Tag: $Tag" -ForegroundColor Cyan
if ($Registry) {
    Write-Host "Registry: $Registry" -ForegroundColor Cyan
}
Write-Host "Images to build: $($images.Count)" -ForegroundColor Cyan
Write-Host ""

# Check network connectivity to Microsoft Container Registry
Write-Host "Checking network connectivity to Microsoft Container Registry..." -ForegroundColor Yellow
try {
    $dnsResult = Resolve-DnsName -Name mcr.microsoft.com -ErrorAction Stop
    Write-Host "OK DNS resolution successful" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Cannot resolve mcr.microsoft.com - this may cause build failures" -ForegroundColor Yellow
    Write-Host "  Error: $_" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Check your internet connection" -ForegroundColor White
    Write-Host "2. Verify DNS settings: nslookup mcr.microsoft.com" -ForegroundColor White
    Write-Host "3. Check if behind a corporate firewall/proxy" -ForegroundColor White
    Write-Host "4. Try: docker pull mcr.microsoft.com/dotnet/aspnet:9.0-alpine" -ForegroundColor White
    Write-Host "5. If using VPN, ensure it's connected" -ForegroundColor White
    Write-Host ""
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        Write-Host "Build cancelled." -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

$failedBuilds = @()

foreach ($img in $images) {
    $fullImageName = if ($Registry) {
        "$Registry/$($img.ImageName):$Tag"
    } else {
        "$($img.ImageName):$Tag"
    }
    
    Write-Host "`nBuilding $($img.Name)..." -ForegroundColor Yellow
    Write-Host "  Image: $fullImageName" -ForegroundColor Gray
    Write-Host "  Dockerfile: $($img.Dockerfile)" -ForegroundColor Gray
    
    try {
        docker build `
            -t $fullImageName `
            -f $img.Dockerfile `
            .
        
        if ($LASTEXITCODE -ne 0) {
            throw "Docker build failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "  ✓ Successfully built $fullImageName" -ForegroundColor Green
        
        if ($Push) {
            Write-Host "  Pushing $fullImageName..." -ForegroundColor Yellow
            docker push $fullImageName
            if ($LASTEXITCODE -ne 0) {
                throw "Docker push failed with exit code $LASTEXITCODE"
            }
            Write-Host "  ✓ Successfully pushed $fullImageName" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "  ✗ Failed to build $fullImageName" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
        $failedBuilds += $img.Name
    }
}

Pop-Location

Write-Host "`n" -NoNewline
Write-Host "Build Summary:" -ForegroundColor Cyan
$totalImages = $images.Count
$successfulCount = $totalImages - $failedBuilds.Count
Write-Host "  Total images: $totalImages" -ForegroundColor Gray
Write-Host "  Successful: $successfulCount" -ForegroundColor Green
if ($failedBuilds.Count -gt 0) {
    Write-Host "  Failed: $($failedBuilds.Count)" -ForegroundColor Red
    Write-Host "  Failed images: $($failedBuilds -join ', ')" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting failed builds:" -ForegroundColor Yellow
    Write-Host "  - Check network connectivity (DNS, firewall, proxy)" -ForegroundColor White
    Write-Host "  - Verify Docker can pull base images: docker pull mcr.microsoft.com/dotnet/aspnet:9.0-alpine" -ForegroundColor White
    Write-Host "  - Check Docker Desktop network settings" -ForegroundColor White
    Write-Host "  - Review Docker build logs above for specific errors" -ForegroundColor White
    exit 1
} else {
    Write-Host "  All images built successfully!" -ForegroundColor Green
    exit 0
}
