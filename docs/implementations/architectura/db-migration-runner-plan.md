# Database Migration Runner - Implementation Plan

## Status: NOT IMPLEMENTED
**Current State**: Skeleton code exists but core services are missing. Identity module migrations will NOT work.

---

## Problems Identified

### 1. Missing Service Implementations
**File**: `server\src\MigrationRunner\Program.cs`

```csharp
// ❌ These interfaces are registered but DON'T EXIST:
services.AddSingleton<IModuleDiscovery, ModuleDiscovery>();
services.AddSingleton<IDependencyGraphResolver, DependencyGraphResolver>();
services.AddSingleton<IMigrationOrchestrator, MigrationOrchestrator>();
```

**Impact**: Application crashes on startup with "Service not found" error.

---

### 2. No FluentMigrator Configuration
**Missing**: Database provider setup, migration runner, connection string handling.

**Impact**: Even if services existed, no migrations would run.

---

### 3. No Module Discovery Logic
**Missing**: Logic to scan and load module migration assemblies.

**Impact**: Identity.Migrations assembly won't be discovered or loaded.

---

### 4. No Configuration
**Missing**: `appsettings.json` with connection strings, topology, module list.

**Impact**: No database to connect to, no modules to run.

---

## Implementation Tasks

### Phase 1: Core Infrastructure (Priority: HIGH)

#### Task 1.1: Create Configuration Files
**File**: `server\src\MigrationRunner\appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "FluentMigrator": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen_dev;Username=datarizen;Password=datarizen"
  },
  "Database": {
    "Provider": "Postgres"
  },
  "Deployment": {
    "Topology": "Monolith"
  },
  "Topologies": {
    "Monolith": {
      "Modules": [
        "Identity",
        "TenantManagement",
        "AppBuilder"
      ]
    }
  }
}
```

**File**: `server\src\MigrationRunner\appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen_dev;Username=datarizen;Password=datarizen"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "FluentMigrator": "Trace"
    }
  }
}
```

**Acceptance Criteria**:
- ✅ Configuration files load successfully
- ✅ Connection string is accessible via `IConfiguration`
- ✅ Topology and module list are defined

---

#### Task 1.2: Update Project File
**File**: `server\src\MigrationRunner\MigrationRunner.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentMigrator.Runner" Version="5.2.0" />
    <PackageReference Include="FluentMigrator.Runner.Postgres" Version="5.2.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.CommandLine" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference module migration projects -->
    <ProjectReference Include="..\Modules\Identity\Identity.Migrations\Identity.Migrations.csproj" />
    <ProjectReference Include="..\Modules\TenantManagement\TenantManagement.Migrations\TenantManagement.Migrations.csproj" />
    <ProjectReference Include="..\Modules\AppBuilder\AppBuilder.Migrations\AppBuilder.Migrations.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="appsettings.Development.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

**Acceptance Criteria**:
- ✅ FluentMigrator packages installed
- ✅ Module migration projects referenced
- ✅ Configuration files copied to output directory

---

#### Task 1.3: Create ModuleInfo Model
**File**: `server\src\MigrationRunner\Models\ModuleInfo.cs`

```csharp
using System.Reflection;

namespace MigrationRunner.Models;

public class ModuleInfo
{
    public required string Name { get; init; }
    public required Assembly MigrationAssembly { get; init; }
    public required string SchemaName { get; init; }
    public string? Description { get; init; }
}
```

**Acceptance Criteria**:
- ✅ Model compiles
- ✅ Required properties enforced

---

#### Task 1.4: Implement IModuleDiscovery
**File**: `server\src\MigrationRunner\Services\IModuleDiscovery.cs`

```csharp
using MigrationRunner.Models;

namespace MigrationRunner.Services;

public interface IModuleDiscovery
{
    IEnumerable<ModuleInfo> DiscoverModules(string topology);
}
```

**File**: `server\src\MigrationRunner\Services\ModuleDiscovery.cs`

```csharp
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MigrationRunner.Models;

namespace MigrationRunner.Services;

public class ModuleDiscovery : IModuleDiscovery
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModuleDiscovery> _logger;

    public ModuleDiscovery(IConfiguration configuration, ILogger<ModuleDiscovery> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IEnumerable<ModuleInfo> DiscoverModules(string topology)
    {
        _logger.LogInformation("Discovering modules for topology: {Topology}", topology);

        // Get module list from configuration
        var moduleNames = _configuration
            .GetSection($"Topologies:{topology}:Modules")
            .Get<string[]>() ?? Array.Empty<string>();

        if (moduleNames.Length == 0)
        {
            _logger.LogWarning("No modules found in topology: {Topology}", topology);
            return Enumerable.Empty<ModuleInfo>();
        }

        var modules = new List<ModuleInfo>();

        foreach (var moduleName in moduleNames)
        {
            try
            {
                // Load migration assembly for module
                var assemblyName = $"{moduleName}.Migrations";
                var assembly = Assembly.Load(assemblyName);

                var moduleInfo = new ModuleInfo
                {
                    Name = moduleName,
                    MigrationAssembly = assembly,
                    SchemaName = ConvertToSchemaName(moduleName)
                };

                modules.Add(moduleInfo);
                _logger.LogInformation("Discovered module: {Module} (Assembly: {Assembly})", 
                    moduleName, assemblyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load migration assembly for module: {Module}", moduleName);
                throw;
            }
        }

        return modules;
    }

    private static string ConvertToSchemaName(string moduleName)
    {
        // Convert "TenantManagement" -> "tenant_management"
        return string.Concat(moduleName.Select((c, i) => 
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
```

**Acceptance Criteria**:
- ✅ Reads module list from `Topologies:{topology}:Modules` configuration
- ✅ Loads migration assembly for each module
- ✅ Converts module name to schema name (e.g., "Identity" → "identity")
- ✅ Logs discovery process
- ✅ Throws exception if assembly not found

**Test**:
```csharp
// Given: appsettings.json has ["Identity", "TenantManagement"]
var modules = moduleDiscovery.DiscoverModules("Monolith");

// Then:
Assert.Equal(2, modules.Count());
Assert.Contains(modules, m => m.Name == "Identity");
Assert.Contains(modules, m => m.SchemaName == "identity");
```

---

#### Task 1.5: Implement IMigrationOrchestrator
**File**: `server\src\MigrationRunner\Services\IMigrationOrchestrator.cs`

```csharp
namespace MigrationRunner.Services;

public interface IMigrationOrchestrator
{
    Task MigrateAsync(bool dryRun = false);
    Task RollbackAsync(bool dryRun = false);
}
```

**File**: `server\src\MigrationRunner\Services\MigrationOrchestrator.cs`

```csharp
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MigrationRunner.Models;

namespace MigrationRunner.Services;

public class MigrationOrchestrator : IMigrationOrchestrator
{
    private readonly IModuleDiscovery _moduleDiscovery;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MigrationOrchestrator> _logger;

    public MigrationOrchestrator(
        IModuleDiscovery moduleDiscovery,
        IConfiguration configuration,
        ILogger<MigrationOrchestrator> logger)
    {
        _moduleDiscovery = moduleDiscovery;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MigrateAsync(bool dryRun = false)
    {
        var topology = _configuration["Deployment:Topology"] ?? "Monolith";
        var modules = _moduleDiscovery.DiscoverModules(topology);

        _logger.LogInformation("Starting migrations for {Count} modules (DryRun: {DryRun})", 
            modules.Count(), dryRun);

        foreach (var module in modules)
        {
            await RunMigrationForModuleAsync(module, dryRun, rollback: false);
        }

        _logger.LogInformation("All migrations completed successfully");
    }

    public async Task RollbackAsync(bool dryRun = false)
    {
        var topology = _configuration["Deployment:Topology"] ?? "Monolith";
        var modules = _moduleDiscovery.DiscoverModules(topology).Reverse();

        _logger.LogInformation("Starting rollback for {Count} modules (DryRun: {DryRun})", 
            modules.Count(), dryRun);

        foreach (var module in modules)
        {
            await RunMigrationForModuleAsync(module, dryRun, rollback: true);
        }

        _logger.LogInformation("All rollbacks completed successfully");
    }

    private async Task RunMigrationForModuleAsync(ModuleInfo module, bool dryRun, bool rollback)
    {
        _logger.LogInformation("Processing module: {Module} (Schema: {Schema})", 
            module.Name, module.SchemaName);

        var serviceProvider = CreateMigrationServiceProvider(module);

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (dryRun)
        {
            _logger.LogInformation("[DRY RUN] Would execute migrations for {Module}", module.Name);
            
            // List pending migrations
            var versionLoader = scope.ServiceProvider.GetRequiredService<IVersionLoader>();
            var versionInfo = versionLoader.VersionInfo;
            
            _logger.LogInformation("Applied versions: {Versions}", 
                string.Join(", ", versionInfo.AppliedMigrations().Select(v => v.ToString())));
        }
        else
        {
            if (rollback)
            {
                _logger.LogInformation("Rolling back last migration for {Module}", module.Name);
                runner.Rollback(1);
            }
            else
            {
                _logger.LogInformation("Migrating up for {Module}", module.Name);
                runner.MigrateUp();
            }
        }

        await Task.CompletedTask;
    }

    private IServiceProvider CreateMigrationServiceProvider(ModuleInfo module)
    {
        var services = new ServiceCollection();

        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(module.MigrationAssembly).For.Migrations()
                .WithVersionTable(new ModuleVersionTableMetaData(module.SchemaName)))
            .AddLogging(lb => lb.AddConsole());

        return services.BuildServiceProvider(validateScopes: false);
    }

    // Custom version table per module schema
    private class ModuleVersionTableMetaData : IVersionTableMetaData
    {
        private readonly string _schemaName;

        public ModuleVersionTableMetaData(string schemaName)
        {
            _schemaName = schemaName;
        }

        public object ApplicationContext { get; set; } = null!;
        public bool OwnsSchema => true;
        public string SchemaName => _schemaName;
        public string TableName => "__FluentMigrator_VersionInfo";
        public string ColumnName => "Version";
        public string DescriptionColumnName => "Description";
        public string UniqueIndexName => "UC_Version";
        public string AppliedOnColumnName => "AppliedOn";
    }
}
```

**Acceptance Criteria**:
- ✅ Discovers modules from topology configuration
- ✅ Creates FluentMigrator runner for each module
- ✅ Configures Postgres provider
- ✅ Uses module-specific schema for version table
- ✅ Supports dry-run mode
- ✅ Supports rollback
- ✅ Logs all operations

**Test**:
```csharp
// Given: Identity.Migrations has 3 migrations
await orchestrator.MigrateAsync(dryRun: false);

// Then:
// - identity schema created
// - identity.__FluentMigrator_VersionInfo table created
// - All 3 migrations applied
```

---

#### Task 1.6: Remove Unused Services
**File**: `server\src\MigrationRunner\Program.cs`

```csharp
using MigrationRunner.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
var rollback = args.Contains("--rollback", StringComparer.OrdinalIgnoreCase);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        var basePath = AppContext.BaseDirectory;
        config.AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: false, reloadOnChange: false);
        config.AddJsonFile(Path.Combine(basePath, $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), optional: true, reloadOnChange: false);
        
        var topologyIndex = Array.FindIndex(args, a => a.Equals("--topology", StringComparison.OrdinalIgnoreCase));
        if (topologyIndex >= 0 && topologyIndex + 1 < args.Length)
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deployment:Topology"] = args[topologyIndex + 1]
            });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IModuleDiscovery, ModuleDiscovery>();
        services.AddSingleton<IMigrationOrchestrator, MigrationOrchestrator>();
        // ❌ REMOVED: IDependencyGraphResolver (not needed - modules are independent)
    })
    .Build();

var orchestrator = host.Services.GetRequiredService<IMigrationOrchestrator>();

try
{
    if (rollback)
        await orchestrator.RollbackAsync(dryRun);
    else
        await orchestrator.MigrateAsync(dryRun);
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Migration failed.");
    Environment.ExitCode = 1;
}
```

**Changes**:
- ❌ Removed `IDependencyGraphResolver` registration (not needed)
- ✅ Added environment-specific appsettings loading
- ✅ Kept topology override via `--topology` argument

**Acceptance Criteria**:
- ✅ Application starts without errors
- ✅ Services resolve correctly
- ✅ Configuration loads from appsettings.json

---

### Phase 2: Module Migrations (Priority: HIGH)

#### Task 2.1: Create Identity.Migrations Project
**File**: `server\src\Modules\Identity\Identity.Migrations\Identity.Migrations.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentMigrator" Version="5.2.0" />
  </ItemGroup>
</Project>
```

**Acceptance Criteria**:
- ✅ Project compiles
- ✅ FluentMigrator package installed

---

#### Task 2.2: Create Initial Identity Migration
**File**: `server\src\Modules\Identity\Identity.Migrations\Migrations\20250115100000_CreateIdentitySchema.cs`

```csharp
using FluentMigrator;

namespace Identity.Migrations.Migrations;

[Migration(20250115100000)]
public class CreateIdentitySchema : Migration
{
    public override void Up()
    {
        Create.Schema("identity");
        
        Create.Table("users")
            .InSchema("identity")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("username").AsString(100).NotNullable().Unique()
            .WithColumn("password_hash").AsString(500).NotNullable()
            .WithColumn("first_name").AsString(100).Nullable()
            .WithColumn("last_name").AsString(100).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        Create.Index("idx_users_email")
            .OnTable("users").InSchema("identity")
            .OnColumn("email");
    }

    public override void Down()
    {
        Delete.Table("users").InSchema("identity");
        Delete.Schema("identity");
    }
}
```

**Acceptance Criteria**:
- ✅ Migration compiles
- ✅ Up() creates schema and users table
- ✅ Down() drops table and schema
- ✅ Follows naming convention: `YYYYMMDDHHmmss_Description.cs`

---

### Phase 3: Testing (Priority: HIGH)

#### Task 3.1: Manual Test - Run Identity Migrations
**Prerequisites**:
- PostgreSQL running on localhost:5432
- Database `datarizen_dev` exists
- User `datarizen` with password `datarizen` has access

**Test Steps**:
```bash
# 1. Build solution
cd C:\git\dr\poc
dotnet build

# 2. Run migrations
cd server\src\MigrationRunner
dotnet run -- --environment Development

# 3. Verify in PostgreSQL
psql -h localhost -U datarizen -d datarizen_dev

# Check schema exists
\dn

# Check version table
SELECT * FROM identity.__FluentMigrator_VersionInfo;

# Check users table
\d identity.users

# Check data
SELECT * FROM identity.users;
```

**Expected Results**:
- ✅ `identity` schema created
- ✅ `identity.__FluentMigrator_VersionInfo` table created with 1 row (version 20250115100000)
- ✅ `identity.users` table created with correct columns
- ✅ No errors in console output

---

#### Task 3.2: Test Dry-Run Mode
```bash
dotnet run -- --environment Development --dry-run
```

**Expected Results**:
- ✅ Logs show "[DRY RUN] Would execute migrations for Identity"
- ✅ No changes to database
- ✅ Lists pending migrations

---

#### Task 3.3: Test Rollback
```bash
# Apply migration
dotnet run -- --environment Development

# Rollback
dotnet run -- --environment Development --rollback

# Verify
psql -h localhost -U datarizen -d datarizen_dev -c "\d identity.users"
# Should show: relation "identity.users" does not exist
```

**Expected Results**:
- ✅ Users table dropped
- ✅ Version table shows no applied migrations
- ✅ Schema still exists (not dropped by rollback)

---

### Phase 4: Additional Features (Priority: MEDIUM)

#### Task 4.1: Add Module-Specific Arguments
**File**: `server\src\MigrationRunner\Program.cs`

```csharp
// Parse --module argument
var moduleIndex = Array.FindIndex(args, a => a.Equals("--module", StringComparison.OrdinalIgnoreCase));
string? specificModule = null;
if (moduleIndex >= 0 && moduleIndex + 1 < args.Length)
{
    specificModule = args[moduleIndex + 1];
}

// Pass to orchestrator
if (rollback)
    await orchestrator.RollbackAsync(dryRun, specificModule);
else
    await orchestrator.MigrateAsync(dryRun, specificModule);
```

**Update IMigrationOrchestrator**:
```csharp
Task MigrateAsync(bool dryRun = false, string? specificModule = null);
Task RollbackAsync(bool dryRun = false, string? specificModule = null);
```

**Update MigrationOrchestrator**:
```csharp
public async Task MigrateAsync(bool dryRun = false, string? specificModule = null)
{
    var topology = _configuration["Deployment:Topology"] ?? "Monolith";
    var modules = _moduleDiscovery.DiscoverModules(topology);

    if (specificModule != null)
    {
        modules = modules.Where(m => m.Name.Equals(specificModule, StringComparison.OrdinalIgnoreCase));
        
        if (!modules.Any())
        {
            throw new InvalidOperationException($"Module '{specificModule}' not found in topology '{topology}'");
        }
    }

    // ... rest of implementation
}
```

**Test**:
```bash
# Run only Identity migrations
dotnet run -- --environment Development --module Identity
```

---

#### Task 4.2: Add Profile Support for Environment-Specific Migrations
**File**: `server\src\MigrationRunner\Program.cs`

```csharp
// Parse --profile argument
var profileIndex = Array.FindIndex(args, a => a.Equals("--profile", StringComparison.OrdinalIgnoreCase));
string? profile = null;
if (profileIndex >= 0 && profileIndex + 1 < args.Length)
{
    profile = args[profileIndex + 1];
}

// Pass to orchestrator
if (rollback)
    await orchestrator.RollbackAsync(dryRun, specificModule, profile);
else
    await orchestrator.MigrateAsync(dryRun, specificModule, profile);
```

**Update IMigrationOrchestrator**:
```csharp
Task MigrateAsync(bool dryRun = false, string? specificModule = null, string? profile = null);
Task RollbackAsync(bool dryRun = false, string? specificModule = null, string? profile = null);
```

**Update MigrationOrchestrator.CreateMigrationServiceProvider**:
```csharp
private IServiceProvider CreateMigrationServiceProvider(ModuleInfo module, string? profile)
{
    var services = new ServiceCollection();

    var connectionString = _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

    services
        .AddFluentMigratorCore()
        .ConfigureRunner(rb =>
        {
            rb.AddPostgres()
              .WithGlobalConnectionString(connectionString)
              .ScanIn(module.MigrationAssembly).For.Migrations();

            // ✅ Configure profile/tags if specified
            if (!string.IsNullOrEmpty(profile))
            {
                rb.WithTags(profile);
            }

            rb.WithVersionTable(new ModuleVersionTableMetaData(module.SchemaName));
        })
        .AddLogging(lb => lb.AddConsole());

    return services.BuildServiceProvider(validateScopes: false);
}
```

**Update RunMigrationForModuleAsync signature**:
```csharp
private async Task RunMigrationForModuleAsync(ModuleInfo module, bool dryRun, bool rollback, string? profile)
{
    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    _logger.LogInformation("Module: {Module}", module.Name);
    _logger.LogInformation("Schema: {Schema}", module.SchemaName);
    _logger.LogInformation("Profile: {Profile}", profile ?? "(none)");
    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

    var serviceProvider = CreateMigrationServiceProvider(module, profile);

    // ... rest of implementation
}
```

**Update MigrateAsync and RollbackAsync**:
```csharp
public async Task MigrateAsync(bool dryRun = false, string? specificModule = null, string? profile = null)
{
    var topology = _configuration["Deployment:Topology"] ?? "Monolith";
    var modules = _moduleDiscovery.DiscoverModules(topology);

    if (specificModule != null)
    {
        modules = modules.Where(m => m.Name.Equals(specificModule, StringComparison.OrdinalIgnoreCase));
        
        if (!modules.Any())
        {
            throw new InvalidOperationException($"Module '{specificModule}' not found in topology '{topology}'");
        }
    }

    _logger.LogInformation("Starting migrations for {Count} modules (DryRun: {DryRun}, Profile: {Profile})", 
        modules.Count(), dryRun, profile ?? "(none)");

    foreach (var module in modules)
    {
        await RunMigrationForModuleAsync(module, dryRun, rollback: false, profile);
    }

    _logger.LogInformation("All migrations completed successfully");
}

public async Task RollbackAsync(bool dryRun = false, string? specificModule = null, string? profile = null)
{
    var topology = _configuration["Deployment:Topology"] ?? "Monolith";
    var modules = _moduleDiscovery.DiscoverModules(topology).Reverse();

    if (specificModule != null)
    {
        modules = modules.Where(m => m.Name.Equals(specificModule, StringComparison.OrdinalIgnoreCase));
        
        if (!modules.Any())
        {
            throw new InvalidOperationException($"Module '{specificModule}' not found in topology '{topology}'");
        }
    }

    _logger.LogInformation("Starting rollback for {Count} modules (DryRun: {DryRun}, Profile: {Profile})", 
        modules.Count(), dryRun, profile ?? "(none)");

    foreach (var module in modules)
    {
        await RunMigrationForModuleAsync(module, dryRun, rollback: true, profile);
    }

    _logger.LogInformation("All rollbacks completed successfully");
}
```

**Acceptance Criteria**:
- ✅ `--profile Development` runs only migrations tagged with `[Profile("Development")]`
- ✅ Without `--profile`, all migrations run (default behavior)
- ✅ Profile is logged in verbose output
- ✅ SeedDevelopmentUsers only runs when `--profile Development` is specified

**Test**:
```bash
# Run all migrations including Development profile
dotnet run -- --environment Development --module Identity --profile Development

# Run only production migrations (no profile-tagged migrations)
dotnet run -- --environment Production --module Identity

# Verify SeedDevelopmentUsers only ran in first case
psql -h localhost -U datarizen -d datarizen_dev -c "SELECT * FROM identity.users WHERE email LIKE '%@dev.local';"
```

**Example Migration Using Profile**:
```csharp
using FluentMigrator;

namespace Identity.Migrations.Migrations;

[Migration(20250115200000)]
[Profile("Development")]  // ✅ Only runs when --profile Development
public class SeedDevelopmentUsers : Migration
{
    public override void Up()
    {
        Insert.IntoTable("users").InSchema("identity")
            .Row(new
            {
                id = Guid.NewGuid(),
                email = "admin@dev.local",
                username = "admin",
                password_hash = "hashed_password",
                is_active = true
            });
    }

    public override void Down()
    {
        Delete.FromTable("users").InSchema("identity")
            .Row(new { email = "admin@dev.local" });
    }
}
```

---

#### Task 4.3: Add Verbose Logging
**File**: `server\src\MigrationRunner\appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "FluentMigrator": "Debug",
      "MigrationRunner": "Debug"
    }
  }
}
```

**Update MigrationOrchestrator**:
```csharp
private async Task RunMigrationForModuleAsync(ModuleInfo module, bool dryRun, bool rollback, string? profile)
{
    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    _logger.LogInformation("Module: {Module}", module.Name);
    _logger.LogInformation("Schema: {Schema}", module.SchemaName);
    _logger.LogInformation("Profile: {Profile}", profile ?? "(none)");
    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

    var serviceProvider = CreateMigrationServiceProvider(module, profile);

    using var scope = serviceProvider.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    if (dryRun)
    {
        _logger.LogInformation("[DRY RUN] Would execute migrations for {Module}", module.Name);
        
        // List pending migrations
        var versionLoader = scope.ServiceProvider.GetRequiredService<IVersionLoader>();
        var versionInfo = versionLoader.VersionInfo;
        
        _logger.LogInformation("Applied versions: {Versions}", 
            string.Join(", ", versionInfo.AppliedMigrations().Select(v => v.ToString())));
    }
    else
    {
        if (rollback)
        {
            _logger.LogInformation("Rolling back last migration for {Module}", module.Name);
            runner.Rollback(1);
        }
        else
        {
            _logger.LogInformation("Migrating up for {Module}", module.Name);
            runner.MigrateUp();
        }
    }

    await Task.CompletedTask;
}
```

---

## Testing Checklist

### Unit Tests (Future)
- [ ] ModuleDiscovery loads modules from configuration
- [ ] ModuleDiscovery converts module names to schema names correctly
- [ ] ModuleDiscovery throws exception if assembly not found
- [ ] MigrationOrchestrator filters by specific module
- [ ] MigrationOrchestrator handles empty module list

### Integration Tests (Future)
- [ ] End-to-end migration run against test database
- [ ] Rollback restores previous state
- [ ] Dry-run doesn't modify database
- [ ] Multiple modules run in sequence

### Manual Tests (NOW)
- [x] Task 3.1: Run Identity migrations
- [x] Task 3.2: Test dry-run mode
- [x] Task 3.3: Test rollback

---

## Success Criteria

### Phase 1 Complete When:
- ✅ `dotnet run` executes without errors
- ✅ Configuration loads from appsettings.json
- ✅ ModuleDiscovery finds Identity module
- ✅ MigrationOrchestrator creates FluentMigrator runner
- ✅ Logs show module discovery and migration execution

### Phase 2 Complete When:
- ✅ Identity.Migrations project compiles
- ✅ Initial migration creates identity schema and users table
- ✅ Migration is discoverable by MigrationRunner

### Phase 3 Complete When:
- ✅ Manual test runs successfully
- ✅ Database has identity schema with users table
- ✅ Version table tracks applied migration
- ✅ Dry-run mode works
- ✅ Rollback works

### Phase 4 Complete When:
- ✅ `--module Identity` runs only Identity migrations
- ✅ Verbose logging shows detailed execution flow

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Assembly not found at runtime | HIGH | Add explicit project references in MigrationRunner.csproj |
| Connection string incorrect | HIGH | Validate connection string on startup |
| Schema already exists | MEDIUM | Use `IfNotExists()` in migrations |
| Migration fails mid-execution | MEDIUM | Wrap in transaction, add error handling |
| Multiple developers run migrations simultaneously | LOW | Use database locks (future enhancement) |

---

## Next Steps After Implementation

1. **Add TenantManagement.Migrations** (same pattern as Identity)
2. **Add AppBuilder.Migrations** (same pattern as Identity)
3. **Add CI/CD pipeline** to run migrations on deployment
4. **Add migration validation** (detect breaking changes)
5. **Add migration testing framework** (automated tests)
6. **Add migration documentation generator** (auto-generate schema docs)

---

## References

- FluentMigrator Documentation: https://fluentmigrator.github.io/
- PostgreSQL Schema Documentation: https://www.postgresql.org/docs/current/ddl-schemas.html
- Migration Best Practices: `docs\ai-context\07-DB-MIGRATION-RUNNER.md`
