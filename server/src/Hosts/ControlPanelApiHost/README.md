# MultiAppControlPanelHost

Host that loads only **TenantManagement** and **Identity** modules (control-panel / admin topology).

## Purpose

- **Multi-app topology**: this process serves tenant and identity management APIs.
- Used alongside MultiAppRuntimeHost and MultiAppAppBuilderHost; typically behind an API gateway.
- Modules are loaded conditionally from `LoadedModules` in configuration.

## Run

```bash
dotnet run --project server/src/Hosts/MultiAppControlPanelHost
```

## Configuration

- `appsettings.json`: `LoadedModules` = `["TenantManagement", "Identity"]`.
- Override via environment or launch profile to change which modules load.

## Next steps

- Register capabilities (multi-tenancy, auth, etc.) when implemented.
- Map controllers or minimal APIs for Tenant and Identity.
