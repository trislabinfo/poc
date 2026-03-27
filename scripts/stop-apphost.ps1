# PowerShell script to stop all AppHost processes
# This script finds and stops all processes related to AppHost to release file locks

param(
    [switch]$Force = $true
)

$ErrorActionPreference = "Stop"

Write-Host "=== Stopping AppHost Processes ===" -ForegroundColor Cyan
Write-Host ""

# Find AppHost processes
$appHostProcesses = Get-Process -Name "AppHost" -ErrorAction SilentlyContinue

if ($appHostProcesses) {
    Write-Host "Found $($appHostProcesses.Count) AppHost process(es):" -ForegroundColor Yellow
    $appHostProcesses | ForEach-Object {
        Write-Host "  - PID $($_.Id): $($_.Path)" -ForegroundColor Yellow
    }
    Write-Host ""
    
    if ($Force) {
        Write-Host "Stopping all AppHost processes..." -ForegroundColor Yellow
        $appHostProcesses | ForEach-Object {
            try {
                Stop-Process -Id $_.Id -Force -ErrorAction Stop
                Write-Host "  ✓ Stopped PID $($_.Id)" -ForegroundColor Green
            } catch {
                Write-Host "  ✗ Failed to stop PID $($_.Id): $_" -ForegroundColor Red
            }
        }
        
        Start-Sleep -Seconds 2
        
        # Verify they're stopped
        $remaining = Get-Process -Name "AppHost" -ErrorAction SilentlyContinue
        if ($remaining) {
            Write-Host ""
            Write-Host "WARNING: Some processes are still running:" -ForegroundColor Yellow
            $remaining | ForEach-Object {
                Write-Host "  - PID $($_.Id): $($_.Path)" -ForegroundColor Yellow
            }
            Write-Host ""
            Write-Host "Try running as Administrator or manually kill these processes." -ForegroundColor Yellow
            exit 1
        } else {
            Write-Host ""
            Write-Host "✓ All AppHost processes stopped successfully" -ForegroundColor Green
        }
    } else {
        $response = Read-Host "Stop these processes? (Y/N)"
        if ($response -eq "Y" -or $response -eq "y") {
            $appHostProcesses | Stop-Process -Force
            Write-Host "✓ Processes stopped" -ForegroundColor Green
        } else {
            Write-Host "Processes not stopped" -ForegroundColor Yellow
            exit 0
        }
    }
} else {
    Write-Host "✓ No AppHost processes found" -ForegroundColor Green
}

# Also check for dotnet processes that might be running AppHost
Write-Host ""
Write-Host "Checking for dotnet processes that might be running AppHost..." -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    try {
        $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
        $cmdLine -like "*AppHost*" -or $cmdLine -like "*AppHost.csproj*"
    } catch {
        $false
    }
}

if ($dotnetProcesses) {
    Write-Host "Found $($dotnetProcesses.Count) dotnet process(es) that might be running AppHost:" -ForegroundColor Yellow
    $dotnetProcesses | ForEach-Object {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
            Write-Host "  - PID $($_.Id): $cmdLine" -ForegroundColor Yellow
        } catch {
            Write-Host "  - PID $($_.Id): (unable to get command line)" -ForegroundColor Yellow
        }
    }
    Write-Host ""
    
    if ($Force) {
        Write-Host "Stopping dotnet processes..." -ForegroundColor Yellow
        $dotnetProcesses | ForEach-Object {
            try {
                Stop-Process -Id $_.Id -Force -ErrorAction Stop
                Write-Host "  ✓ Stopped PID $($_.Id)" -ForegroundColor Green
            } catch {
                Write-Host "  ✗ Failed to stop PID $($_.Id): $_" -ForegroundColor Red
            }
        }
        Write-Host ""
        Write-Host "✓ All related processes stopped" -ForegroundColor Green
    } else {
        $response = Read-Host "Stop these dotnet processes? (Y/N)"
        if ($response -eq "Y" -or $response -eq "y") {
            $dotnetProcesses | Stop-Process -Force
            Write-Host "✓ Processes stopped" -ForegroundColor Green
        }
    }
} else {
    Write-Host "✓ No related dotnet processes found" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host "You can now run: aspire publish -e k8s -o k8s-artifacts" -ForegroundColor Gray
