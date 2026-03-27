# BuildingBlocks.Migrations

Shared utilities for module migration projects (Identity, Tenant, User, Feature).

## SeedDataLoader

Loads seed data from **embedded JSON files** using an environment-based lookup:

1. Try `{resourcePrefix}.{Environment}.{fileName}` (e.g. `Datarizen.Identity.Migrations.SeedData.Development.users.json`)
2. Fall back to `{resourcePrefix}.Common.{fileName}` (e.g. `Datarizen.Identity.Migrations.SeedData.Common.roles.json`)

### Usage (from a module's migration)

1. Reference this project from your module's Migrations project.
2. Embed your JSON files with logical names like `{YourNamespace}.SeedData.Common.roles.json` and `{YourNamespace}.SeedData.Development.users.json`.
3. In the migration:

```csharp
using System.Reflection;
using Datarizen.BuildingBlocks.Migrations;

var assembly = Assembly.GetExecutingAssembly();
const string resourcePrefix = "Datarizen.Identity.Migrations.SeedData"; // your module's prefix

var roles = SeedDataLoader.Load<RoleSeedDto>(assembly, resourcePrefix, "roles.json");
var users = SeedDataLoader.Load<UserSeedDto>(assembly, resourcePrefix, "users.json"); // uses GetEnvironment()
```

### GetEnvironment()

`SeedDataLoader.GetEnvironment()` returns, in order:

- `ASPNETCORE_ENVIRONMENT`
- `DOTNET_ENVIRONMENT`
- `"Development"`

Use this when loading environment-specific seed (e.g. `users.json`).
