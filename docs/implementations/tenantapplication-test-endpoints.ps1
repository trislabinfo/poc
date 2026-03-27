# Test TenantApplication endpoints (AppHost must be running; monolith on https://localhost:8443)
$base = "https://localhost:8443"
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

function Rest-Ok { param($msg) Write-Host "OK. $msg" -ForegroundColor Green }
function Rest-Err { param($e) Write-Host "Error: $($e.Exception.Message)" -ForegroundColor Red; if ($e.ErrorDetails.Message) { Write-Host $e.ErrorDetails.Message -ForegroundColor Red } }

# 1. Create or get tenant
Write-Host "`n=== 1. POST /api/tenant (create tenant) ===" -ForegroundColor Cyan
$tenantSlug = "test-tenant-" + (Get-Date -Format "HHmmss")
$tenantBody = '{"Name":"Test Tenant","Slug":"' + $tenantSlug + '"}'
try {
    $tenantResp = Invoke-RestMethod -Uri "$base/api/tenant" -Method Post -Body $tenantBody -ContentType "application/json" -TimeoutSec 15
    $tenant = if ($tenantResp.data) { $tenantResp.data } else { $tenantResp }
    $tenantId = if ($tenant.id) { $tenant.id } else { $tenant.Id }
    Rest-Ok "Tenant ID: $tenantId"
} catch { Rest-Err $_; exit 1 }

# 2. GET applications for tenant
Write-Host "`n=== 2. GET /api/tenantapplication/tenants/{id}/applications ===" -ForegroundColor Cyan
try {
    $apps = Invoke-RestMethod -Uri "$base/api/tenantapplication/tenants/$tenantId/applications" -Method Get -TimeoutSec 15
    Rest-Ok "Count: $($apps.Count)"
} catch { Rest-Err $_ }

# 3. POST custom application
Write-Host "`n=== 3. POST /api/tenantapplication/tenants/{id}/applications/custom ===" -ForegroundColor Cyan
$customSlug = "custom-app-" + (Get-Date -Format "HHmmss")
$customBody = '{"Name":"Custom Test App","Slug":"' + $customSlug + '","Description":"Test"}'
try {
    $taResp = Invoke-RestMethod -Uri "$base/api/tenantapplication/tenants/$tenantId/applications/custom" -Method Post -Body $customBody -ContentType "application/json" -TimeoutSec 15
    $ta = if ($taResp.data) { $taResp.data } else { $taResp }
    $tenantApplicationId = if ($ta.id) { $ta.id } else { $ta.Id }
    Rest-Ok "TenantApplication ID: $tenantApplicationId"
} catch { Rest-Err $_; exit 1 }

# 4. GET releases
Write-Host "`n=== 4. GET .../applications/{tenantApplicationId}/releases ===" -ForegroundColor Cyan
try {
    $releases = Invoke-RestMethod -Uri "$base/api/tenantapplication/tenants/$tenantId/applications/$tenantApplicationId/releases" -Method Get -TimeoutSec 15
    Rest-Ok "Releases: $($releases.Count)"
} catch { Rest-Err $_ }

# 5. POST create release
Write-Host "`n=== 5. POST .../applications/{tenantApplicationId}/releases ===" -ForegroundColor Cyan
$releaseBody = '{"Major":1,"Minor":0,"Patch":0,"ReleaseNotes":"Tenant release"}'
try {
    $release = Invoke-RestMethod -Uri "$base/api/tenantapplication/tenants/$tenantId/applications/$tenantApplicationId/releases" -Method Post -Body $releaseBody -ContentType "application/json" -TimeoutSec 15
    Rest-Ok "Release ID: $($release.id)"
} catch { Rest-Err $_ }

# 6. POST create environment
Write-Host "`n=== 6. POST .../applications/{tenantApplicationId}/environments ===" -ForegroundColor Cyan
$envBody = '{"Name":"Development","EnvironmentType":0}'
try {
    $envResp = Invoke-RestMethod -Uri "$base/api/tenantapplication/tenants/$tenantId/applications/$tenantApplicationId/environments" -Method Post -Body $envBody -ContentType "application/json" -TimeoutSec 15
    $envObj = if ($envResp.data) { $envResp.data } else { $envResp }
    $envId = if ($envObj.id) { $envObj.id } else { $envObj.Id }; Rest-Ok "Environment ID: $envId"
} catch { Rest-Err $_ }

# 7. GET environments
Write-Host "`n=== 7. GET .../applications/{tenantApplicationId}/environments ===" -ForegroundColor Cyan
try {
    $envs = Invoke-RestMethod -Uri "$base/api/tenantapplication/tenants/$tenantId/applications/$tenantApplicationId/environments" -Method Get -TimeoutSec 15
    Rest-Ok "Environments: $($envs.Count)"
} catch { Rest-Err $_ }

Write-Host "`nDone." -ForegroundColor Cyan
