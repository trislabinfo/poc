# Kill processes listening on server-related ports (ApiGateway, AppHost, service hosts).
# Usage: .\kill-server-ports.ps1
#        .\kill-server-ports.ps1 -KillAllDotNet   # also kill all dotnet.exe processes
param([switch]$KillAllDotNet)

$ports = @(
    64229, 64230,   # ApiGateway
    8433,            # AppHost
    57021, 57022,    # TenantApplicationServiceHost
    57041, 57042,    # AppRuntimeServiceHost
    57001, 57002,    # IdentityServiceHost
    56797, 56801,    # MultiAppAppBuilderHost
    56799, 56800,    # MultiAppControlPanelHost
    60450, 60451,    # AppBuilderServiceHost
    50443, 50080,    # MonolithHost
    56798, 56802,    # MultiAppRuntimeHost
    57011, 57012,    # TenantServiceHost
    60942, 60943     # RuntimeBFFHost
)

$pidsToKill = @{}
Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $ports -contains $_.LocalPort } |
    ForEach-Object {
        $pidsToKill[$_.OwningProcess] = $true
        $p = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        Write-Host "Port $($_.LocalPort): PID $($_.OwningProcess) - $($p.ProcessName)"
    }

foreach ($procId in $pidsToKill.Keys) {
    try {
        Stop-Process -Id $procId -Force
        Write-Host "Killed PID $procId"
    } catch {
        Write-Warning "Could not kill PID $procId : $_"
    }
}

if ($pidsToKill.Count -eq 0) {
    Write-Host "No processes found listening on server ports."
}

if ($KillAllDotNet) {
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Killing dotnet PID $($_.Id)"
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Done killing dotnet processes."
}
