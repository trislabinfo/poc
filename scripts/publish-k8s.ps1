# PowerShell script to publish Aspire AppHost to Kubernetes
# This script ensures no AppHost processes are running before publishing

param(
    [string]$OutputPath = "k8s-artifacts",
    [string]$Environment = "k8s"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Aspire Kubernetes Publish ===" -ForegroundColor Cyan
Write-Host ""

# Check if Aspire CLI is installed
Write-Host "Checking Aspire CLI..." -ForegroundColor Yellow
try {
    $aspireVersion = aspire --version 2>&1
    Write-Host "OK Aspire CLI found: $aspireVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Aspire CLI not found. Installing..." -ForegroundColor Red
    dotnet tool install -g Aspire.Cli
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install Aspire CLI" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK Aspire CLI installed" -ForegroundColor Green
}

# Check for running AppHost processes
Write-Host ""
Write-Host "Checking for running AppHost processes..." -ForegroundColor Yellow
$appHostProcesses = Get-Process -Name "AppHost" -ErrorAction SilentlyContinue

if ($appHostProcesses) {
    Write-Host "WARNING: Found $($appHostProcesses.Count) running AppHost process(es):" -ForegroundColor Yellow
    $appHostProcesses | ForEach-Object {
        Write-Host "  - PID $($_.Id): $($_.Path)" -ForegroundColor Yellow
    }
    Write-Host ""
    $response = Read-Host "Stop these processes before publishing? (Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        $appHostProcesses | ForEach-Object {
            Write-Host "Stopping AppHost process PID $($_.Id)..." -ForegroundColor Yellow
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Seconds 2
        Write-Host "OK AppHost processes stopped" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Cannot publish while AppHost is running. Please stop AppHost processes manually." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "OK No running AppHost processes found" -ForegroundColor Green
}

# Check Docker connectivity
Write-Host ""
Write-Host "Checking Docker connectivity..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "OK Docker is running" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Docker may not be running or accessible" -ForegroundColor Yellow
    Write-Host "  The publish command may fail if Docker is required" -ForegroundColor Yellow
}

# Navigate to AppHost directory
$appHostDir = Join-Path $PSScriptRoot "..\server\src\AppHost"
if (-not (Test-Path $appHostDir)) {
    Write-Host "ERROR: AppHost directory not found: $appHostDir" -ForegroundColor Red
    exit 1
}

Push-Location $appHostDir

try {
    Write-Host ""
    Write-Host "Publishing to Kubernetes..." -ForegroundColor Cyan
    Write-Host "  Environment: $Environment" -ForegroundColor Gray
    Write-Host "  Output Path: $OutputPath" -ForegroundColor Gray
    Write-Host ""
    
    # Run aspire publish with correct parameters
    aspire publish -e $Environment -o $OutputPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=== Publish Successful ===" -ForegroundColor Green
        Write-Host "Kubernetes artifacts generated in: $OutputPath" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "  1. Review generated manifests in $OutputPath" -ForegroundColor Gray
        Write-Host "  2. Build Docker images: .\scripts\build-images.ps1 -Topology <Topology>" -ForegroundColor Gray
        Write-Host "  3. Deploy to minikube or AKS" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "=== Publish Failed ===" -ForegroundColor Red
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
