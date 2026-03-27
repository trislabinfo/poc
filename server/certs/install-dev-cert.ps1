# One-time setup for developers: imports the team dev code-signing certificate
# so Windows trusts our signed DLLs and the build can sign assemblies.
# Run from server/certs: .\install-dev-cert.ps1

$ErrorActionPreference = "Stop"
$CertDir = $PSScriptRoot
$PfxPath = Join-Path $CertDir "dev-code-signing.pfx"

if (-not (Test-Path $PfxPath)) {
    Write-Host "Missing $PfxPath. Pull the repo (certs are committed) or ask a teammate to run create-dev-cert.ps1 and commit the cert."
    exit 1
}

$Password = if ($env:DEV_SIGNING_CERT_PASSWORD) { $env:DEV_SIGNING_CERT_PASSWORD } else { "dev" }
$SecurePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

Write-Host "Importing dev code-signing certificate to Personal store and Trusted Publishers..."
$cert = Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation "Cert:\CurrentUser\My" -Password $SecurePassword

$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host "Done. You can build now; Tenant.Migrations.dll (and other signed outputs) will be trusted by Windows."
Write-Host "Set DEV_SIGNING_CERT_PASSWORD if you use a different password (default: dev)."
