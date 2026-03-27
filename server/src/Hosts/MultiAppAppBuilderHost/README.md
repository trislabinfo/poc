# MultiAppAppBuilderHost

Host that loads only the **FeatureManagement** module (app-builder / configuration topology).

## Purpose

- **Multi-app topology**: this process serves feature and feature-flag management APIs.
- Used for configuration and app-builder scenarios; typically behind an API gateway.
- Modules loaded from `LoadedModules`; default is `["FeatureManagement"]`.

## Run

```bash
dotnet run --project server/src/Hosts/MultiAppAppBuilderHost
```

## Configuration

- `appsettings.json`: `LoadedModules` = `["FeatureManagement"]`.

## Next steps

- Register capabilities and map Feature module endpoints.
