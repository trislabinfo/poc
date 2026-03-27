# AppHost Microservices Topology Update

## Overview

This document describes the updates needed to `server/src/AppHost/Program.cs` to support the new microservice hosts for the Feature, AppBuilder, and AppRuntime modules in the Microservices deployment topology.

---

## Current State

The current AppHost configuration supports three topologies:
- **MultiTenantMonolith**: Single monolith host with all modules
- **MultiTenantMultiApp**: Multiple app hosts (ControlPanel, AppBuilder, Runtime) + API Gateway
- **Microservices**: Currently uses monolith as placeholder + API Gateway

---

## Required Changes

### 1. Add New Microservice Hosts

Update the `Microservices` topology case in `server/src/AppHost/Program.cs` to include dedicated microservice hosts for:
- **FeatureServiceHost**: Feature and FeatureFlag management
- **AppBuilderServiceHost**: Application building and configuration
- **AppRuntimeServiceHost**: Application runtime management
- **TenantServiceHost**: Tenant management (already exists)
- **IdentityServiceHost**: Identity and authentication (already exists)

### 2. Updated AppHost Program.cs

**File**: `server/src/AppHost/Program.cs`

```csharp
// ... existing code ...

switch (topology)
{
    case "MultiTenantMonolith":
        builder.AddProject("monolith", "../Hosts/MonolithHost/Monolith.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);
        break;

    case "MultiTenantMultiApp":
        builder.AddProject("controlpanel", "../Hosts/MultiAppControlPanelHost/MultiApp.ControlPanel.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);
        builder.AddProject("runtime", "../Hosts/MultiAppRuntimeHost/MultiApp.Runtime.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);
        builder.AddProject("appbuilder", "../Hosts/MultiAppAppBuilderHost/MultiApp.AppBuilder.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);
        builder.AddProject("gateway", "../ApiGateway/ApiGateway.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId);
        break;

    case "Microservices":
        // Tenant Service
        builder.AddProject("tenant-service", "../Hosts/TenantServiceHost/Tenant.Service.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);

        // Identity Service
        builder.AddProject("identity-service", "../Hosts/IdentityServiceHost/Identity.Service.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);

        // Feature Service (NEW)
        builder.AddProject("feature-service", "../Hosts/FeatureServiceHost/Feature.Service.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);

        // AppBuilder Service (NEW)
        builder.AddProject("appbuilder-service", "../Hosts/AppBuilderServiceHost/AppBuilder.Service.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);

        // AppRuntime Service (NEW)
        builder.AddProject("appruntime-service", "../Hosts/AppRuntimeServiceHost/AppRuntime.Service.Host.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(rabbitMq);

        // API Gateway
        builder.AddProject("gateway", "../ApiGateway/ApiGateway.csproj")
            .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId);
        break;

    default:
        throw new InvalidOperationException($"Unknown topology: {topology}. Use Monolith, MultiTenantMultiApp, or Microservices.");
}

await builder.Build().RunAsync();
```

---

## API Gateway Port Configuration

The API Gateway listens on **port 5000** (HTTP) and **port 5001** (HTTPS) by default.

**File**: `server/src/ApiGateway/Properties/launchSettings.json`

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Aspire Configuration**: When running via .NET Aspire, the API Gateway is automatically assigned a port by Aspire's orchestration. The port 5000 is used for local development outside of Aspire.

---

## Service Discovery & Communication

### Service Names

In the Microservices topology, services will be discoverable via:
- `tenant-service`: http://tenant-service
- `identity-service`: http://identity-service
- `feature-service`: http://feature-service
- `appbuilder-service`: http://appbuilder-service
- `appruntime-service`: http://appruntime-service
- `gateway`: http://gateway (port 5000 in local dev, Aspire-assigned in orchestrated mode)

### API Gateway Routing

The API Gateway should route requests to the appropriate microservice:

**File**: `server/src/ApiGateway/appsettings.json` (update)

```json
{
  "ReverseProxy": {
    "Routes": {
      "tenant-route": {
        "ClusterId": "tenant-cluster",
        "Match": {
          "Path": "/api/tenant/{**catch-all}"
        }
      },
      "identity-route": {
        "ClusterId": "identity-cluster",
        "Match": {
          "Path": "/api/identity/{**catch-all}"
        }
      },
      "feature-route": {
        "ClusterId": "feature-cluster",
        "Match": {
          "Path": "/api/feature/{**catch-all}"
        }
      },
      "appbuilder-route": {
        "ClusterId": "appbuilder-cluster",
        "Match": {
          "Path": "/api/appbuilder/{**catch-all}"
        }
      },
      "appruntime-route": {
        "ClusterId": "appruntime-cluster",
        "Match": {
          "Path": "/api/appruntime/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "tenant-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://tenant-service"
          }
        }
      },
      "identity-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://identity-service"
          }
        }
      },
      "feature-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://feature-service"
          }
        }
      },
      "appbuilder-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://appbuilder-service"
          }
        }
      },
      "appruntime-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://appruntime-service"
          }
        }
      }
    }
  }
}
```

---

## Migration Runner Updates

Update the MigrationRunner configuration to include the new modules in the Microservices topology.

**File**: `server/src/MigrationRunner/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=datarizen;Username=postgres;Password=postgres"
  },
  "Deployment": {
    "Topology": "Monolith"
  },
  "MigrationRunner": {
    "ModulesByTopology": {
      "Monolith": [ "Tenant", "Identity", "Feature", "TenantApplication", "AppBuilder", "AppRuntime" ],
      "MultiTenantMultiApp": [ "Tenant", "Identity", "Feature", "TenantApplication", "AppBuilder", "AppRuntime" ],
      "Microservices": [ "Tenant", "Identity", "Feature", "TenantApplication", "AppBuilder", "AppRuntime" ]
    }
  }
}
```

**Note**: The "User" module is part of the Identity module and does not have a separate migration. User-related tables are created by Identity module migrations.

---

## Service Dependencies

### Migration Order

The modules must be migrated in dependency order:
1. **Tenant** (no dependencies)
2. **Identity** (depends on Tenant) - includes User tables
3. **Feature** (depends on Tenant)
4. **TenantApplication** (depends on Tenant)
5. **AppBuilder** (depends on Tenant, Feature)
6. **AppRuntime** (depends on Tenant, TenantApplication, AppBuilder)

### MigrationRunner Discovery Mechanism

The MigrationRunner discovers and executes migrations using the following mechanism:

1. **Module Discovery**:
   - Reads `ModulesByTopology` configuration from `appsettings.json`
   - Loads modules in the order specified for the current topology
   - Each module name corresponds to a `{ModuleName}.Migrations` assembly

2. **Assembly Loading**:
   - For each module (e.g., "Tenant"), loads the assembly `{ModuleName}.Migrations.dll`
   - Example: "Tenant" → `Tenant.Migrations.dll`

3. **Migration Discovery**:
   - Uses FluentMigrator's assembly scanner to find all classes that inherit from `Migration`
   - Migrations are identified by their `[Migration(timestamp)]` attribute
   - Example: `[Migration(20260101120000)]`

4. **Execution Order**:
   - Migrations within a module are executed in timestamp order (oldest first)
   - Modules are executed in the order specified in `ModulesByTopology`
   - FluentMigrator tracks executed migrations in `VersionInfo` table

5. **Schema Isolation**:
   - Each module's migrations create and use their own schema
   - Example: Tenant module uses `tenant` schema, Feature module uses `feature` schema
   - Schema name is specified in each migration using `.WithSchema("schema_name")`

**Example Migration Discovery Flow**:
```
1. Load "Tenant" module → Tenant.Migrations.dll
2. Scan for Migration classes → Find CreateTenantsTable, AddTenantSlug, etc.
3. Execute in timestamp order → 20260101120000, 20260102130000, etc.
4. Load "Identity" module → Identity.Migrations.dll
5. Repeat for all modules in order
```

### Runtime Dependencies

- **FeatureServiceHost**: Depends on Tenant module for tenant context
- **AppBuilderServiceHost**: Depends on Tenant and Feature modules
- **AppRuntimeServiceHost**: Depends on Tenant, Feature, and AppBuilder modules

---

## Health Checks

Each microservice host should expose health check endpoints following .NET Aspire conventions.

### Health Check Endpoint Convention

**Path**: `/health`
**Format**: JSON response compatible with .NET Aspire health check format

**Example Response** (Healthy):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "PostgreSQL connection is healthy",
      "duration": "00:00:00.0100000"
    },
    "redis": {
      "status": "Healthy",
      "description": "Redis connection is healthy",
      "duration": "00:00:00.0020000"
    }
  }
}
```

**Example Response** (Unhealthy):
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Unhealthy",
      "description": "PostgreSQL connection failed",
      "exception": "Npgsql.NpgsqlException: Connection refused",
      "duration": "00:00:00.0100000"
    }
  }
}
```

### Health Check Endpoints

Each microservice exposes its health check at:

- `http://tenant-service/health`
- `http://identity-service/health`
- `http://feature-service/health`
- `http://appbuilder-service/health`
- `http://appruntime-service/health`

### API Gateway Health Aggregation

The API Gateway can aggregate these health checks for overall system health at `/health` endpoint.

---

## Testing the Microservices Topology

### 1. Update AppHost Configuration

**File**: `server/src/AppHost/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Deployment": {
    "Topology": "Microservices"
  }
}
```

### 2. Run the AppHost

```bash
cd server/src/AppHost
dotnet run
```

This will start:
- PostgreSQL container
- Redis container
- RabbitMQ container
- Tenant Service
- Identity Service
- Feature Service
- AppBuilder Service
- AppRuntime Service
- API Gateway

### 3. Verify Services

Open the Aspire Dashboard (typically at https://localhost:17134) to verify all services are running.

### 4. Test API Endpoints

```bash
# Test Tenant Service
curl http://localhost:5000/api/tenant/tenants

# Test Feature Service
curl http://localhost:5000/api/feature/features

# Test AppBuilder Service
curl http://localhost:5000/api/appbuilder/applications/tenant/{tenantId}

# Test AppRuntime Service
curl http://localhost:5000/api/appruntime/instances/application/{applicationId}
```

---

## Deployment Considerations

### Database

- **Shared Database**: All microservices connect to the same PostgreSQL database but use schema isolation
  - `tenant` schema for Tenant module
  - `identity` schema for Identity module
  - `feature` schema for Feature module
  - `appbuilder` schema for AppBuilder module
  - `appruntime` schema for AppRuntime module

- **Separate Databases** (optional): Each microservice can have its own database for complete isolation
  - Requires updating connection strings per service
  - Requires distributed transactions or saga pattern for cross-service operations

### Service Communication

- **HTTP/REST**: Services communicate via HTTP REST APIs through the API Gateway
- **gRPC** (future): Can be added for high-performance inter-service communication
- **Message Bus**: RabbitMQ for asynchronous event-driven communication

### Observability

- **Logging**: Structured logging with Serilog to centralized logging system
- **Metrics**: Prometheus metrics exposed by each service
- **Tracing**: Distributed tracing with OpenTelemetry
- **Health Checks**: Regular health checks for service availability

---

## Summary of Changes

### New Files Created

1. `server/src/Hosts/FeatureServiceHost/Feature.Service.Host.csproj`
2. `server/src/Hosts/FeatureServiceHost/Program.cs`
3. `server/src/Hosts/FeatureServiceHost/appsettings.json`
4. `server/src/Hosts/AppBuilderServiceHost/AppBuilder.Service.Host.csproj`
5. `server/src/Hosts/AppBuilderServiceHost/Program.cs`
6. `server/src/Hosts/AppBuilderServiceHost/appsettings.json`
7. `server/src/Hosts/AppRuntimeServiceHost/AppRuntime.Service.Host.csproj`
8. `server/src/Hosts/AppRuntimeServiceHost/Program.cs`
9. `server/src/Hosts/AppRuntimeServiceHost/appsettings.json`

### Files Updated

1. `server/src/AppHost/Program.cs` - Added microservice hosts to Microservices topology
2. `server/src/ApiGateway/appsettings.json` - Added routing for new services
3. `server/src/MigrationRunner/appsettings.json` - Added new modules to migration order

---

## Next Steps

1. **Implement the code** based on the documentation created:
   - Feature module (44 hours)
   - AppBuilder module (48 hours)
   - AppRuntime module (46 hours)

2. **Create the microservice hosts** as documented

3. **Update AppHost and API Gateway** as documented

4. **Test all three topologies**:
   - Monolith
   - MultiTenantMultiApp
   - Microservices

5. **Performance testing** to compare topologies

6. **Documentation** for deployment and operations

---

## Estimated Implementation Time

| Component | Time |
|-----------|------|
| Feature Module | 44 hours |
| AppBuilder Module | 48 hours |
| AppRuntime Module | 46 hours |
| Microservice Hosts | 6 hours |
| AppHost Updates | 2 hours |
| API Gateway Updates | 2 hours |
| Testing & Validation | 8 hours |
| **Total** | **156 hours** |

---

## Notes

- All three deployment topologies (Monolith, MultiApp, Microservices) share the same codebase
- The topology is selected via configuration (`Deployment:Topology`)
- Schema isolation ensures data separation even in shared database scenarios
- The Microservices topology provides the most flexibility for scaling and deployment
- Start with Monolith for development, move to Microservices for production


