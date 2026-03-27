# Dev code-signing certificate

Used so Windows trusts our built assemblies (e.g. `Tenant.Migrations.dll`) and doesn’t block MSBuild.

## One-time setup (first time on this repo)

**Who creates the cert (once per repo):**

1. Open PowerShell and go to `server/certs`.
2. Run: `.\create-dev-cert.ps1`
3. Commit the generated files:
   - `dev-code-signing.pfx`
   - `dev-cert-thumbprint.txt`

**Every other developer (after pulling):**

1. Open PowerShell as the user that will build (no need for Admin).
2. Go to `server/certs`.
3. Run: `.\install-dev-cert.ps1`

After that, builds will sign the migration DLL and Windows will trust it.

## Password

- The PFX password defaults to **`dev`**.
- To use another password: set env var **`DEV_SIGNING_CERT_PASSWORD`** before running the scripts and before building.
- The build uses the certificate from your **Personal** store (installed by the scripts); the thumbprint is read from `dev-cert-thumbprint.txt`.

## Will it work on other machines?

Yes. Everyone uses the **same** certificate (the committed `.pfx`). Each developer runs **`install-dev-cert.ps1` once** on their machine to install that cert into their **Personal** and **Trusted Publishers** stores. After that, builds sign with the same thumbprint and Windows trusts the signed DLLs on every machine.
