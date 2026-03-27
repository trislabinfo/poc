// Aspire typically gets these from `Properties/launchSettings.json`.
// If the AppHost is started without a launch profile (e.g. `dotnet run --project ...`),
// these environment variables may be missing and Aspire will fail fast with an
// OptionsValidationException. Provide safe local defaults to keep `dotnet run` working.
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "https://localhost:17134;http://localhost:15170");
}

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL")))
{
    Environment.SetEnvironmentVariable("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL", "https://localhost:22057");
}

var hasOtlpGrpc = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"));
var hasOtlpHttp = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"));
if (!hasOtlpGrpc && !hasOtlpHttp)
{
    Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "https://localhost:21030");
}

// When running under Aspire, the child projects are built and executed from their MSBuild output directories.
// If you also build the solution in VS/CLI, Windows file locking can cause MSBuild copy retries (MSB3026).
// To avoid that, we force Aspire-launched projects to use an isolated output path per AppHost run.
var aspireRunId = Environment.GetEnvironmentVariable("DATARIZEN_ASPIRE_RUN_ID")
    ?? $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

var datarizenConst = "dr";
var postgresImageTag = "16.6-alpine"; // alpne OS (PostGIS, pgvector and TimescaleDB not available)
string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() ?? "development";
var datarizenEnv = string.Join('-', datarizenConst, environment);

// Check if we're publishing to Kubernetes (bind mounts are not supported)
// When 'aspire publish -e k8s' runs, ASPNETCORE_ENVIRONMENT is set to "k8s"
// Also check command-line arguments for --environment k8s or -e k8s
var isKubernetesPublish = false;
if (environment == "k8s")
{
    isKubernetesPublish = true;
}
else
{
    // Check command-line arguments
    for (int i = 0; i < args.Length; i++)
    {
        if ((args[i].Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
             args[i].Equals("-e", StringComparison.OrdinalIgnoreCase)) &&
            i + 1 < args.Length &&
            args[i + 1].Equals("k8s", StringComparison.OrdinalIgnoreCase))
        {
            isKubernetesPublish = true;
            break;
        }
    }
}

if (environment.Equals("production"))
{
    postgresImageTag = "16.6"; // debian OS
}

var builder = DistributedApplication.CreateBuilder(args);

// Add Kubernetes environment (optional - only used when publishing to K8s)
// This does not affect Aspire's local development behavior
var k8s = builder.AddKubernetesEnvironment("k8s")
    .WithProperties(k8s =>
    {
        k8s.HelmChartName = "datarizen";
    });

// Use datarizen/datarizen! when running via Aspire so Postgres container and connection string match (same as k8s).
// User secrets (Parameters:postgres-username, Parameters:postgres-password) override when set; otherwise default.
var postgresUsername = builder.AddParameter("postgres-username", () => builder.Configuration["Parameters:postgres-username"] ?? "datarizen");
var postgresPassword = builder.AddParameter("postgres-password", () => builder.Configuration["Parameters:postgres-password"] ?? "datarizen!", secret: true);

// Infrastructure resources (all topologies)
var postgres = builder.AddPostgres(string.Join('-', datarizenEnv, "db"), postgresUsername, postgresPassword)
    .WithImageTag(postgresImageTag)
    //.WithEndpoint(port: 5432, targetPort: 5432, name: "db")
    .WithHostPort(5432)
    .WithDataVolume(string.Join('-', datarizenEnv, "db")) //docker volume name
                                                          // Problem with Manual Environment Variables
                                                          // When you use .AddPostgres(), Aspire already sets POSTGRES_USER, POSTGRES_PASSWORD, and POSTGRES_DB internally.
                                                          //.WithEnvironment("POSTGRES_USER", datarizenEnv)
                                                          //.WithEnvironment("POSTGRES_PASSWORD", datarizenEnv)
                                                          //.WithEnvironment("POSTGRES_DB", datarizenConst)
    .WithLifetime(ContainerLifetime.Persistent) // Container NOT removed when Aspire stops
                                                // Performance tuning
                                                // .WithEnvironment("POSTGRES_SHARED_BUFFERS", "256MB")
                                                // .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "200")
                                                // .WithEnvironment("POSTGRES_WORK_MEM", "4MB")
                                                // Logging
                                                // .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8 --locale=en_US.UTF-8")
                                                // Health check
                                                // .WithHealthCheck()
    .PublishAsConnectionString();

postgres.AddDatabase(datarizenEnv, datarizenEnv);

// Standalone pgAdmin: no master password prompt; pre-loaded server list from conf/pgadmin/servers.json
var postgresResourceName = string.Join('-', datarizenEnv, "db");
var appHostProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var pgAdminServersJsonPath = Path.Combine(appHostProjectDir, "conf", "pgadmin", "servers.json");
var pgAdminPgpassPath = Path.Combine(appHostProjectDir, "pgpass");

var pgAdminBuilder = builder.AddContainer(string.Join('-', datarizenEnv, "dbadmin"), "dpage/pgadmin4", "9.12.0")
    .WithHttpEndpoint(port: 5050, targetPort: 80, name: "http")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@datarizen.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", datarizenEnv)
    .WithEnvironment("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False")
    .WithEnvironment("PGADMIN_REPLACE_SERVERS_ON_STARTUP", "True")
    .WithEnvironment("DATARIZEN_PG_HOST", postgresResourceName)
    .WaitFor(postgres);

// Bind mounts are only supported for local Docker development, not Kubernetes publishing
// For Kubernetes publishing, bind mounts are not supported, so we skip them
// For local Aspire development (Docker), we apply bind mounts
if (!isKubernetesPublish)
{
    if (File.Exists(pgAdminServersJsonPath))
    {
        pgAdminBuilder.WithBindMount(pgAdminServersJsonPath, "/pgadmin4/servers.json", isReadOnly: true);
    }
    if (File.Exists(pgAdminPgpassPath))
    {
        pgAdminBuilder.WithBindMount(pgAdminPgpassPath, "/tmp/pgpass", isReadOnly: true);
    }
}

var redis = builder.AddRedis(string.Join('-', datarizenEnv, "cache"))
    .WithImageTag("7.4-alpine")
    //.WithEndpoint(port: 6379, targetPort: 6379, name: "cache")
    .WithHostPort(6379)
    .WithDataVolume(string.Join('-', datarizenEnv, "cache"))
    //.WithLifetime(ContainerLifetime.Persistent)
    // For more control, mount custom config files
    // .WithBindMount(Path.Combine(configPath, "redis.conf"), "/usr/local/etc/redis/redis.conf", isReadOnly: true)
    // .WithEnvironment("REDIS_PASSWORD", builder.Configuration["Redis:Password"] ?? "dev-password")
    // .WithArgs("redis-server", "/usr/local/etc/redis/redis.conf")
    // Security
    // .WithEnvironment("REDIS_PASSWORD", "your-strong-password-here")  // Require authentication
    // .WithEnvironment("REDIS_DISABLE_COMMANDS", "FLUSHDB,FLUSHALL,CONFIG")  // Disable dangerous commands
    // Performance
    // .WithEnvironment("REDIS_MAXMEMORY", "2gb")  // Set memory limit
    // .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru")  // Eviction policy
    // .WithEnvironment("REDIS_SAVE", "900 1 300 10 60 10000")  // Persistence (RDB snapshots)
    // .WithEnvironment("REDIS_APPENDONLY", "yes")  // Enable AOF for durability
    // .WithEnvironment("REDIS_APPENDFSYNC", "everysec")  // AOF sync strategy
    // Connection limits
    // .WithEnvironment("REDIS_MAXCLIENTS", "10000")  // Max concurrent connections
    // .WithEnvironment("REDIS_TIMEOUT", "300")  // Close idle connections after 5 min
    // Performance tuning
    // .WithEnvironment("REDIS_TCP_BACKLOG", "511")  // TCP connection queue
    // .WithEnvironment("REDIS_TCP_KEEPALIVE", "300")  // TCP keepalive
    .WithRedisCommander(c => c  // ← Redis web UI
        .WithImageTag("latest")
        .WithHostPort(6060));

// RabbitMQ with credentials from user secrets
var rabbitmqUsername = builder.AddParameter("rabbitmq-username");
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", secret: true);

var rabbitMq = builder.AddRabbitMQ(string.Join('-', datarizenEnv, "messaging"), rabbitmqUsername, rabbitmqPassword)
    .WithImageTag("4.0-management-alpine")
    .WithManagementPlugin(7070)
    .WithDataVolume(string.Join('-', datarizenEnv, "messaging"));
//.WithEnvironment("RABBITMQ_DEFAULT_USER", datarizenEnv) // TODO: remove for porduction use secrets
//.WithEnvironment("RABBITMQ_DEFAULT_PASS", datarizenEnv); // TODO: remove for porduction use secrets
// Security
// .WithEnvironment("RABBITMQ_DEFAULT_USER", "admin")  // Change from 'guest'
// .WithEnvironment("RABBITMQ_DEFAULT_PASS", "your-strong-password-here")
// .WithEnvironment("RABBITMQ_DEFAULT_VHOST", "datarizen")  // Custom vhost
// Performance - Memory
// .WithEnvironment("RABBITMQ_VM_MEMORY_HIGH_WATERMARK", "0.6")  // Use 60% of RAM before blocking
// .WithEnvironment("RABBITMQ_VM_MEMORY_HIGH_WATERMARK_PAGING_RATIO", "0.75")  // Start paging at 75%
// Performance - Disk
// .WithEnvironment("RABBITMQ_DISK_FREE_LIMIT", "2GB")  // Min free disk space
// Connection limits
// .WithEnvironment("RABBITMQ_MAX_CONNECTIONS", "1000")  // Max concurrent connections
// .WithEnvironment("RABBITMQ_MAX_CHANNELS", "2000")  // Max channels per connection
// Heartbeat
// .WithEnvironment("RABBITMQ_HEARTBEAT", "60")  // Heartbeat interval (seconds)
// Performance - Prefetch
// .WithEnvironment("RABBITMQ_CHANNEL_MAX", "2047")  // Max channels
// Clustering (if needed)
// .WithEnvironment("RABBITMQ_ERLANG_COOKIE", "your-secret-cookie-here")  // For clustering
//.WithLifetime(ContainerLifetime.Persistent);

// Topology: Monolith | DistributedApp | Microservices
// Command-line override (e.g. --topology Microservices or --Deployment__Topology=Microservices) takes precedence.
static string? GetTopologyFromArgs(string[] a)
{
    for (var i = 0; i < a.Length; i++)
    {
        if (a[i].Equals("--topology", StringComparison.OrdinalIgnoreCase) && i + 1 < a.Length)
            return a[i + 1];
        if (a[i].StartsWith("--Deployment__Topology=", StringComparison.OrdinalIgnoreCase))
            return a[i].Split('=', 2)[1].Trim();
        if (a[i].StartsWith("--Deployment:Topology=", StringComparison.OrdinalIgnoreCase))
            return a[i].Split('=', 2)[1].Trim();
    }
    return null;
}
var topology = GetTopologyFromArgs(args) ?? builder.Configuration["Deployment:Topology"];

switch (topology)
{
    case "Monolith":
        {
            // Use fixed ports outside ephemeral range. isProxied: false so the app binds directly.
            var monolith = builder.AddProject("monolith", "../Hosts/MonolithHost/Monolith.Host.csproj")
                .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "monolithHttp", isProxied: false)
                .WithHttpsEndpoint(port: 8443, targetPort: 8443, name: "monolithHttps", isProxied: false)
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("ASPNETCORE_URLS", "https://localhost:8443;http://localhost:8080")
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq);
            // Runtime BFF (for runtime client): optimizes communication; aggregates data from Monolith (or in-process when distributed).
            var monolithRuntimeBff = builder.AddProject("runtimebff", "../Hosts/RuntimeBFFHost/RuntimeBFFHost.csproj")
                .WithHttpEndpoint(port: 56803, targetPort: 56803, name: "runtimebffHttp", isProxied: false)
                .WithHttpsEndpoint(port: 56799, targetPort: 56799, name: "runtimebffHttps", isProxied: false)
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("Services__monolith__http", monolith.GetEndpoint("monolithHttp"))
                .WithEnvironment("Services__identity__http", monolith.GetEndpoint("monolithHttp"))
                .WithReference(monolith);
            // Client apps: dashboard and builder call monolith directly; runtime calls Runtime BFF.
            // Use HTTP endpoint for dashboard/builder to avoid browser blocking untrusted HTTPS cert on localhost.
            var monolithRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
            var monolithClientAppsRoot = Path.Combine(monolithRepoRoot, "client", "apps");
            var monolithBuilderPath = Path.Combine(monolithClientAppsRoot, "builder");
            var monolithDashboardPath = Path.Combine(monolithClientAppsRoot, "dashboard");
            var monolithRuntimeAppPath = Path.Combine(monolithClientAppsRoot, "runtime");
            builder.AddJavaScriptApp("builder", monolithBuilderPath)
                .WithPnpm(install: false)
                .WithWorkingDirectory(monolithBuilderPath)
                .WithHttpEndpoint(port: 5173, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", monolith.GetEndpoint("monolithHttp"));
            builder.AddJavaScriptApp("dashboard", monolithDashboardPath)
                .WithPnpm(install: false)
                .WithWorkingDirectory(monolithDashboardPath)
                .WithHttpEndpoint(port: 5174, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", monolith.GetEndpoint("monolithHttp"));
            builder.AddJavaScriptApp("runtime", monolithRuntimeAppPath)
                .WithPnpm(install: false)
                .WithWorkingDirectory(monolithRuntimeAppPath)
                .WithHttpEndpoint(port: 5175, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", monolithRuntimeBff.GetEndpoint("runtimebffHttps"));
            break;
        }

    case "DistributedApp":
        {
            var controlpanel = builder.AddProject("controlpanel", "../Hosts/MultiAppControlPanelHost/DistributedApp.ControlPanel.Host.csproj")
                .WithHttpEndpoint(port: 8081, targetPort: 81, name: "controlpanelHttp")
                .WithHttpsEndpoint(port: 8444, targetPort: 8444, name: "controlpanelHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq);
            var runtime = builder.AddProject("runtime", "../Hosts/MultiAppRuntimeHost/DistributedApp.Runtime.Host.csproj")
                .WithHttpEndpoint(port: 56802, targetPort: 56802, name: "runtimeHttp")
                .WithHttpsEndpoint(port: 56798, targetPort: 56798, name: "runtimeHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("LoadedModules__0", "TenantManagement")
                .WithEnvironment("LoadedModules__1", "TenantApplication")
                .WithEnvironment("LoadedModules__2", "AppBuilder")
                .WithEnvironment("LoadedModules__3", "AppRuntime")
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(controlpanel)
                .WithEnvironment("Services__identity__http", controlpanel.GetEndpoint("controlpanelHttp"));
            var appbuilder = builder.AddProject("appbuilder", "../Hosts/MultiAppAppBuilderHost/DistributedApp.AppBuilder.Host.csproj")
                .WithHttpEndpoint(port: 56801, targetPort: 56801, name: "appbuilderHttp")
                .WithHttpsEndpoint(port: 56797, targetPort: 56797, name: "appbuilderHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq);
            var gateway = builder.AddProject("gateway", "../ApiGateway/ApiGateway.csproj")
                .WithHttpEndpoint(name: "gatewayHttp")
                .WithHttpsEndpoint(name: "gatewayHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithReference(controlpanel)
                .WithReference(runtime)
                .WithReference(appbuilder);
            // Client apps (dashboard, builder, runtime) with Runtime BFF via gateway; runtime service runs AppRuntime module.
            var distRepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
            var distClientAppsRoot = Path.Combine(distRepoRoot, "client", "apps");
            builder.AddJavaScriptApp("builder", Path.Combine(distClientAppsRoot, "builder"))
                .WithPnpm(install: false)
                .WithWorkingDirectory(Path.Combine(distClientAppsRoot, "builder"))
                .WithHttpEndpoint(port: 5173, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("https"));
            builder.AddJavaScriptApp("dashboard", Path.Combine(distClientAppsRoot, "dashboard"))
                .WithPnpm(install: false)
                .WithWorkingDirectory(Path.Combine(distClientAppsRoot, "dashboard"))
                .WithHttpEndpoint(port: 5174, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("https"));
            builder.AddJavaScriptApp("runtime", Path.Combine(distClientAppsRoot, "runtime"))
                .WithPnpm(install: false)
                .WithWorkingDirectory(Path.Combine(distClientAppsRoot, "runtime"))
                .WithHttpEndpoint(port: 5175, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("https"));
            break;
        }

    case "Microservices":
        {
            // Identity microservice host (project): Aspire sets URLs via targetPort (dynamic host port via proxy).
            // Use distinct internal ports that won't clash with standalone runs.
            var identity = builder.AddProject("identity", "../Hosts/IdentityServiceHost/Identity.Service.Host.csproj")
                .WithHttpEndpoint(targetPort: 61002, name: "identityHttp")
                .WithHttpsEndpoint(targetPort: 61001, name: "identityHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq);

            // Tenant microservice host (project) — needs Identity URL to create users (CreateTenantWithUsers)
            var tenant = builder.AddProject("tenant", "../Hosts/TenantServiceHost/Tenant.Service.Host.csproj")
                .WithHttpEndpoint(targetPort: 61012, name: "tenantHttp")
                .WithHttpsEndpoint(targetPort: 61011, name: "tenantHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("Services__identity__http", identity.GetEndpoint("identityHttp"))
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(identity);

            // AppBuilder microservice host (project)
            var appbuilder = builder.AddProject("appbuilder", "../Hosts/AppBuilderServiceHost/AppBuilder.Service.Host.csproj")
                .WithHttpEndpoint(targetPort: 61022, name: "appbuilderHttp")
                .WithHttpsEndpoint(targetPort: 61021, name: "appbuilderHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq);

            // TenantApplication microservice host (project) — calls Tenant and AppBuilder via HTTP (contract)
            var tenantapplication = builder.AddProject("tenantapplication", "../Hosts/TenantApplicationServiceHost/TenantApplication.Service.Host.csproj")
                .WithHttpEndpoint(targetPort: 61032, name: "tenantapplicationHttp")
                .WithHttpsEndpoint(targetPort: 61031, name: "tenantapplicationHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("Services__tenant__http", tenant.GetEndpoint("tenantHttp"))
                .WithEnvironment("AppBuilder__BaseUrl", appbuilder.GetEndpoint("appbuilderHttp"))
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(tenant)
                .WithReference(appbuilder);

            // Runtime BFF host (exposes /api/runtime/* for the runtime client; references AppRuntime.BFF)
            var runtimebff = builder.AddProject("runtimebff", "../Hosts/RuntimeBFFHost/RuntimeBFFHost.csproj")
                .WithHttpEndpoint(targetPort: 61042, name: "runtimebffHttp")
                .WithHttpsEndpoint(targetPort: 61041, name: "runtimebffHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("Services__identity__http", identity.GetEndpoint("identityHttp"))
                .WithReference(postgres)
                .WithReference(redis)
                .WithReference(rabbitMq)
                .WithReference(identity);

            // Gateway: use Aspire endpoint references so backend URLs resolve to actual proxy host:port.
            // WaitFor ensures gateway starts after backends so endpoint URLs are allocated before gateway reads them.
            var gateway = builder.AddProject("gateway", "../ApiGateway/ApiGateway.csproj")
                .WithHttpEndpoint(name: "gatewayHttp")
                .WithHttpsEndpoint(name: "gatewayHttps")
                .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
                .WithEnvironment("ReverseProxy__Clusters__tenant__Destinations__destination1__Address", tenant.GetEndpoint("tenantHttp"))
                .WithEnvironment("ReverseProxy__Clusters__identity__Destinations__destination1__Address", identity.GetEndpoint("identityHttp"))
                .WithEnvironment("ReverseProxy__Clusters__tenantapplication__Destinations__destination1__Address", tenantapplication.GetEndpoint("tenantapplicationHttp"))
                .WithEnvironment("ReverseProxy__Clusters__appbuilder__Destinations__destination1__Address", appbuilder.GetEndpoint("appbuilderHttp"))
                .WithEnvironment("ReverseProxy__Clusters__runtime__Destinations__destination1__Address", runtimebff.GetEndpoint("runtimebffHttp"))
                .WithReference(tenant)
                .WithReference(identity)
                .WithReference(tenantapplication)
                .WithReference(appbuilder)
                .WithReference(runtimebff)
                .WaitFor(tenant)
                .WaitFor(identity);

            // Client apps (SvelteKit/Vite); resolve path to repo root from AppHost output (e.g. .../AppHost/bin/Debug/net10.0 -> 6 levels up = repo root)
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
            var clientAppsRoot = Path.Combine(repoRoot, "client", "apps");
            // Client apps: skip Aspire's pnpm install (install: false) to avoid EPERM / exit -4048 on Windows.
            // Run `pnpm install` from repo root (or from client/apps/builder, client/apps/dashboard, client/apps/runtime) before starting AppHost.
            var builderAppPath = Path.Combine(clientAppsRoot, "builder");
            var dashboardAppPath = Path.Combine(clientAppsRoot, "dashboard");
            var runtimeAppPath = Path.Combine(clientAppsRoot, "runtime");
            builder.AddJavaScriptApp("builder", builderAppPath)
                .WithPnpm(install: false)
                .WithWorkingDirectory(builderAppPath)
                .WithHttpEndpoint(port: 5173, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("https"));
            builder.AddJavaScriptApp("dashboard", dashboardAppPath)
                .WithPnpm(install: false)
                .WithWorkingDirectory(dashboardAppPath)
                .WithHttpEndpoint(port: 5174, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("https"));
            builder.AddJavaScriptApp("runtime", runtimeAppPath)
                .WithPnpm(install: false)
                .WithWorkingDirectory(runtimeAppPath)
                .WithHttpEndpoint(port: 5175, env: "PORT")
                .WithEnvironment("VITE_API_BASE_URL", gateway.GetEndpoint("https"));
            break;
        }

    default:
        throw new InvalidOperationException($"Unknown topology: {topology}. Use Monolith, DistributedApp, or Microservices.");
}

//builder.AddProject<Projects.AppRuntime_Client_Host>("runtime-client-host");

await builder.Build().RunAsync();
