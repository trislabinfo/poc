# Test AppBuilder Endpoints
# Usage: .\test-appbuilder-endpoints.ps1

$ErrorActionPreference = "Continue"
$baseUrl = "https://localhost:8443"
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "`n=== Testing AppBuilder Endpoints ===" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl`n" -ForegroundColor Yellow

# Test 1: GET /api/appbuilder/catalog/applications
Write-Host "Test 1: GET /api/appbuilder/catalog/applications" -ForegroundColor Cyan
try {
    $catalog = Invoke-RestMethod -Uri "$baseUrl/api/appbuilder/catalog/applications" -Method Get -TimeoutSec 20
    Write-Host "✓ Success! Found $($catalog.Count) application(s) in catalog" -ForegroundColor Green
    $catalog | ConvertTo-Json -Depth 3
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "  HTTP Status: $statusCode" -ForegroundColor Red
    }
}

Write-Host "`n" -NoNewline

# Test 2: Get applications first (to get an ID)
Write-Host "Test 2: GET /api/appbuilder/applications" -ForegroundColor Cyan
try {
    $apps = Invoke-RestMethod -Uri "$baseUrl/api/appbuilder/applications" -Method Get -TimeoutSec 20
    Write-Host "✓ Success! Found $($apps.Count) application(s)" -ForegroundColor Green
    
    if ($apps.Count -eq 0) {
        Write-Host "  No applications found. Creating one..." -ForegroundColor Yellow
        $body = @{
            Name = "Test Application $(Get-Date -Format 'yyyyMMddHHmmss')"
            Description = "Test app for endpoint testing"
            Slug = "test-app-$(Get-Date -Format 'yyyyMMddHHmmss')"
            IsPublic = $true
        } | ConvertTo-Json
        
        $newApp = Invoke-RestMethod -Uri "$baseUrl/api/appbuilder/applications" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 20
        $appId = $newApp.Id
        Write-Host "  ✓ Created application: $($newApp.Name) (ID: $appId)" -ForegroundColor Green
    } else {
        $appId = $apps[0].Id
        Write-Host "  Using existing application: $($apps[0].Name) (ID: $appId)" -ForegroundColor Yellow
    }
    
    Write-Host "`nTest 3: GET /api/appbuilder/applications/$appId/releases" -ForegroundColor Cyan
    try {
        $releases = Invoke-RestMethod -Uri "$baseUrl/api/appbuilder/applications/$appId/releases" -Method Get -TimeoutSec 20
        Write-Host "✓ Success! Found $($releases.Count) release(s)" -ForegroundColor Green
        $releases | ConvertTo-Json -Depth 5
    } catch {
        Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "`nTest 4: POST /api/appbuilder/applications/$appId/releases" -ForegroundColor Cyan
    try {
        $releaseBody = @{
            Major = 1
            Minor = 0
            Patch = 0
            ReleaseNotes = "Test release created via PowerShell"
        } | ConvertTo-Json
        
        $newRelease = Invoke-RestMethod -Uri "$baseUrl/api/appbuilder/applications/$appId/releases" -Method Post -Body $releaseBody -ContentType "application/json" -TimeoutSec 20
        Write-Host "✓ Success! Created release (ID: $($newRelease.id))" -ForegroundColor Green
        $newRelease | ConvertTo-Json -Depth 3
    } catch {
        Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Response: $responseBody" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "  HTTP Status: $statusCode" -ForegroundColor Red
    }
}

Write-Host "`n=== Tests Complete ===" -ForegroundColor Cyan
