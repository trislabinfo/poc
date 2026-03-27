# MultiAppRuntimeHost

Host that loads only the **UserManagement** module (runtime / end-user topology).

## Purpose

- **Multi-app topology**: this process serves user management APIs for end users.
- Typically used behind an API gateway with MultiAppControlPanelHost and MultiAppAppBuilderHost.
- Modules loaded from `LoadedModules`; default is `["UserManagement"]`.

## Run

```bash
dotnet run --project server/src/Hosts/MultiAppRuntimeHost
```

## Configuration

- `appsettings.json`: `LoadedModules` = `["UserManagement"]`.

## Next steps

- Register capabilities and map User module endpoints.
