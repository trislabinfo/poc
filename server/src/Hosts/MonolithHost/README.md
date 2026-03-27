# MonolithHost

Single deployable host that loads **all** product modules (Tenant, Identity, User, Feature) and all capabilities.

## Purpose

- **Monolith topology**: one process serves all module APIs and capabilities.
- Use for development, demos, or when a single deployment unit is preferred.
- All modules are registered unconditionally at startup.

## Run

```bash
dotnet run --project server/src/Hosts/MonolithHost
```

## Configuration

- `appsettings.json`: connection strings (PostgreSQL, Redis, RabbitMQ) and `Deployment:Topology = Monolith`.
- No `LoadedModules` filter; every module is loaded.

## Troubleshooting

### FileLoadException: "An Application Control policy has blocked this file" (0x800711C7)

Windows (WDAC, AppLocker, or antivirus) is blocking loading a module DLL. Resolve at the policy level:

- **Personal/dev machine:** Add the repo or output path as an exclusion in Windows Security (Virus & threat protection → Manage settings → Exclusions).
- **Corporate/managed device:** Ask IT for an AppLocker/WDAC exception for this project path or for unsigned assemblies.
- **Alternative:** Run the host in WSL or in Docker so it runs under Linux and is not subject to Windows Application Control.

## Next steps

- Add capability registration (multi-tenancy, auth, auditing, feature flags) when those services are implemented.
- Map module endpoints (controllers or minimal APIs) per module.
