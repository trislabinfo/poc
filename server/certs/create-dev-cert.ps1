# Creates a self-signed code-signing certificate for development and exports it
# so the team can use the same cert. Run once (e.g. by the first dev), then
# commit server/certs/dev-code-signing.pfx and server/certs/dev-cert-thumbprint.txt.
# Other devs run install-dev-cert.ps1 instead.

$ErrorActionPreference = "Stop"
$CertDir = $PSScriptRoot
$PfxPath = Join-Path $CertDir "dev-code-signing.pfx"
$ThumbprintPath = Join-Path $CertDir "dev-cert-thumbprint.txt"

# Default password for the dev PFX (used by build and install script)
$Password = if ($env:DEV_SIGNING_CERT_PASSWORD) { $env:DEV_SIGNING_CERT_PASSWORD } else { "dev" }
$SecurePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

if (Test-Path $PfxPath) {
    Write-Host "Certificate already exists at $PfxPath. Delete it first if you want to recreate."
    exit 1
}

Write-Host "Creating self-signed code-signing certificate..."
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=Datarizen Dev Code Signing" `
    -FriendlyName "Datarizen Dev Code Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5)

$Thumbprint = $cert.Thumbprint
Write-Host "Thumbprint: $Thumbprint"

# Export to PFX so the team can install the same cert
Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password $SecurePassword | Out-Null
Set-Content -Path $ThumbprintPath -Value $Thumbprint -NoNewline

# Add to Trusted Publishers so Windows allows loading our signed DLLs
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host ""
Write-Host "Done. Certificate created and added to Trusted Publishers."
Write-Host "  PFX:        $PfxPath"
Write-Host "  Thumbprint: $ThumbprintPath"
Write-Host ""
Write-Host "Commit both files so other devs can run install-dev-cert.ps1."
Write-Host "Build uses thumbprint; signing uses cert from your Personal store (already there)."
Write-Host "Password for PFX: use env DEV_SIGNING_CERT_PASSWORD or default 'dev'."
