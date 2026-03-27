# Datarizen - Development Environment Setup Script (Windows)
# This script initializes user secrets for local development with Aspire

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Datarizen - Development Environment Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to AppHost directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppHostDir = Split-Path -Parent $ScriptDir

Write-Host "Navigating to AppHost directory..." -ForegroundColor Yellow
Set-Location $AppHostDir
Write-Host "Current directory: $(Get-Location)" -ForegroundColor Gray
Write-Host ""

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "OK .NET SDK found: $dotnetVersion" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "Error: .NET SDK is not installed" -ForegroundColor Red
    Write-Host "Please install .NET 10.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Initialize user secrets
Write-Host "Initializing user secrets..." -ForegroundColor Yellow
dotnet user-secrets init
Write-Host "OK User secrets initialized" -ForegroundColor Green
Write-Host ""

# Set PostgreSQL credentials
Write-Host "Setting PostgreSQL credentials..." -ForegroundColor Yellow
dotnet user-secrets set "Parameters:postgres-username" "datarizen"
dotnet user-secrets set "Parameters:postgres-password" "datarizen!"
Write-Host "OK PostgreSQL credentials set" -ForegroundColor Green
Write-Host "  Username: datarizen" -ForegroundColor Gray
Write-Host "  Password: datarizen!" -ForegroundColor Gray
Write-Host ""

# Set RabbitMQ credentials
Write-Host "Setting RabbitMQ credentials..." -ForegroundColor Yellow
dotnet user-secrets set "Parameters:rabbitmq-username" "datarizen"
dotnet user-secrets set "Parameters:rabbitmq-password" "datarizen!"
Write-Host "OK RabbitMQ credentials set" -ForegroundColor Green
Write-Host "  Username: datarizen" -ForegroundColor Gray
Write-Host "  Password: datarizen!" -ForegroundColor Gray
Write-Host ""

# Create pgAdmin pgpass file (so pgAdmin can connect to Postgres without prompting)
# Format: hostname:port:database:username:password (must be one line, no trailing newline in strict mode)
$pgpassPath = Join-Path $AppHostDir "pgpass"
$pgpassLine = "dr-development-db:5432:*:datarizen:datarizen!"
Set-Content -Path $pgpassPath -Value $pgpassLine -NoNewline
Write-Host "OK pgAdmin pgpass file created: $pgpassPath" -ForegroundColor Green
Write-Host "  (pgAdmin will use this to connect to Postgres without asking for password)" -ForegroundColor Gray
Write-Host ""

# Verify secrets
Write-Host "Verifying user secrets..." -ForegroundColor Yellow
Write-Host ""
dotnet user-secrets list
Write-Host ""

# Check Docker
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "OK Docker is running" -ForegroundColor Green
} catch {
    Write-Host "Warning: Docker is not installed or not running" -ForegroundColor Red
    Write-Host "Please install Docker Desktop from https://www.docker.com/products/docker-desktop"
}
Write-Host ""

# Summary
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Ensure Docker Desktop is running"
Write-Host "2. Run the AppHost:"
Write-Host "   cd server\src\AppHost"
Write-Host "   dotnet run"
Write-Host ""
Write-Host "3. Access services:"
Write-Host "   - Aspire Dashboard: http://localhost:15000"
Write-Host "   - API: http://localhost:8080"
Write-Host "   - pgAdmin: Check Aspire Dashboard for URL"
Write-Host "   - Redis Commander: Check Aspire Dashboard for URL"
Write-Host ""
Write-Host "User secrets location:"
Write-Host "  $env:APPDATA\Microsoft\UserSecrets\<user_secrets_id>\secrets.json" -ForegroundColor Gray
Write-Host ""
Write-Host "To view secrets: dotnet user-secrets list"
Write-Host "To modify secrets: dotnet user-secrets set Parameters:key value"
Write-Host ""


