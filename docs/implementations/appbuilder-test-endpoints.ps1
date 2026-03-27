# Test AppBuilder endpoints (AppHost must be running; monolith on https://localhost:8443)
$base = "https://localhost:8443"
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "`n=== 1. GET /api/appbuilder/catalog/applications ===" -ForegroundColor Cyan
try {
    $r = Invoke-RestMethod -Uri "$base/api/appbuilder/catalog/applications" -Method Get -TimeoutSec 15
    Write-Host "OK. Count: $($r.Count)" -ForegroundColor Green
} catch { Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red }

Write-Host "`n=== 2. GET /api/appbuilder/applications ===" -ForegroundColor Cyan
try {
    $apps = Invoke-RestMethod -Uri "$base/api/appbuilder/applications" -Method Get -TimeoutSec 15
    Write-Host "OK. Count: $($apps.Count)" -ForegroundColor Green
    if ($apps.Count -eq 0) {
        $body = '{"Name":"Test App","Description":"Test","Slug":"test-app-' + (Get-Date -Format 'HHmmss') + '","IsPublic":true}'
        $new = Invoke-RestMethod -Uri "$base/api/appbuilder/applications" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 15
        $appId = $new.Id
        Write-Host "Created app ID: $appId" -ForegroundColor Green
    } else {
        $appId = $apps[0].Id
    }
} catch { Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red; exit 1 }

Write-Host "`n=== 3. GET /api/appbuilder/applications/$appId/releases ===" -ForegroundColor Cyan
try {
    $releases = Invoke-RestMethod -Uri "$base/api/appbuilder/applications/$appId/releases" -Method Get -TimeoutSec 15
    Write-Host "OK. Releases: $($releases.Count)" -ForegroundColor Green
} catch { Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red; if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message } }

Write-Host "`n=== 4. POST /api/appbuilder/applications/$appId/releases ===" -ForegroundColor Cyan
try {
    $body = '{"Major":1,"Minor":0,"Patch":0,"ReleaseNotes":"Test"}'
    $r = Invoke-RestMethod -Uri "$base/api/appbuilder/applications/$appId/releases" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 15
    Write-Host "OK. Release ID: $($r.id)" -ForegroundColor Green
} catch { Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red; if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message } }

Write-Host "`nDone." -ForegroundColor Cyan
