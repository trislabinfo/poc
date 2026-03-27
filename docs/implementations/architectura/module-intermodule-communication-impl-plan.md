# Inter-Module Communication: Create Tenant with Tenant Users

**Status**: New  
**Scope**: Tenant ↔ Identity across **Monolith**, **DistributedApp**, and **Microservices** topologies  
**Goal**: When a new tenant is created (with tenant users in the request), persist the tenant in the Tenant service and create corresponding users in the Identity service for authentication and authorization. Same application logic works in all topologies; only the Tenant→Identity communication mechanism changes (in-process vs HTTP). AppHost and each host project are updated accordingly.

---

## 1. Overview

### 1.1 Business flow

- **Create Tenant** accepts: tenant data (name, slug) **and** a list of **tenant users** (e.g. email, display name, password, role/owner flag).
- Each tenant user must exist in **both**:
  - **Tenant module**: tenant–user association (e.g. `tenant_user` with `tenant_id`, `user_id`, `is_tenant_owner`).
  - **Identity module**: user record used for login and authorization.
- One API call from the client triggers: create tenant in Tenant service **and** create users in Identity service. Communication is **in-process** (MediatR) in Monolith and **HTTP** when Tenant and Identity run in separate processes (DistributedApp with split modules, or Microservices).

### 1.2 Topologies

| Topology        | Tenant and Identity in same process? | Tenant → Identity mechanism | AppHost | Host that runs Create Tenant |
|----------------|--------------------------------------|-----------------------------|---------|-----------------------------|
| **Monolith**   | Yes (single host)                    | In-process (MediatR)        | One project: monolith | MonolithHost |
| **DistributedApp** | Depends on LoadedModules (can be same or different host) | In-process if same host; HTTP if different host | controlpanel, runtime, appbuilder, gateway | ControlPanel (if Tenant there) or dedicated |
| **Microservices** | No (separate services)              | HTTP + service discovery    | identity + tenant projects; tenant.WithReference(identity) | Tenant host only |

---

## 2. Architecture

### 2.1 Order of operations (Tenant-owned flow)

1. **Tenant service**: Validate request, create tenant in Tenant DB, (optionally) create local `tenant_user` rows with placeholder or resolved `user_id`s.
2. **Tenant service**: For each tenant user, call **Identity service** to create the user (e.g. `POST /api/identity/create-tenant-user` or `POST /api/identity/users`).
3. **Tenant service**: If Identity returns success, associate returned `user_id` with `tenant_user` and persist (or update) in Tenant DB.
4. **On any Identity failure**: Decide between rollback (delete tenant and any created users) or retry/partial success and clear failure reporting to the client.

This matches your example: *create tenant in TenantService DB first, then notify Identity to create user(s)*.

### 2.2 Communication: Tenant → Identity (abstraction)

- **Abstraction**: `IIdentityApplicationService` (e.g. in a shared contract assembly or Identity.Contracts) with `CreateUserAsync` and optionally `DeleteUserAsync` (for rollback). The create-tenant-with-users handler depends only on this interface.
- **Monolith**: Same process → implement with **in-process** call (e.g. MediatR `CreateUserCommand`). No HTTP.
- **DistributedApp / Microservices**: Different process(es) → implement with **HTTP** using `IHttpClientFactory` and a named client `"identity"`. Base URL from Aspire service discovery (`WithReference(identity)`) or config (`Services:Identity:BaseUrl`).

### 2.3 Abstraction and implementations

**Interface** (e.g. in `Identity.Contracts` or a shared project referenced by Tenant.Application):

```csharp
public interface IIdentityApplicationService
{
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default); // for rollback
}

public record CreateUserRequest(Guid TenantId, string Email, string DisplayName, string? Password, bool IsTenantOwner);
```

- **Monolith (in-process)**: `IdentityApplicationService` in Identity.Application — uses `IMediator.Send(CreateUserCommand(...))`. Register in **MonolithHost** only when `Deployment:Topology` is Monolith (or when both Tenant and Identity modules are loaded in the same host).
- **Distributed (HTTP)**: `IdentityHttpClient` in Identity.Infrastructure (or Tenant.Infrastructure) — uses `IHttpClientFactory.CreateClient("identity")` and `POST /api/identity/create-tenant-user`. Register in hosts that run **Tenant but not Identity** (or when topology is DistributedApp/Microservices and Identity is in another process), and configure the named HttpClient with service discovery or `Services:Identity:BaseUrl`.

### 2.4 Aspire AppHost (summary)

- **Monolith**: One project (e.g. `monolith`). No cross-project reference needed for Identity; same process.
- **DistributedApp**: Today ControlPanel can load both Tenant and Identity → in-process. If you split so Tenant runs in one host and Identity in another, the Tenant host project must `.WithReference(identityHost)` so it gets the Identity URL.
- **Microservices**: Add `identity` and `tenant` as separate projects; `tenant.WithReference(identity)`. Project names are the service names for discovery.

---

## 3. Tenant Service: Create Tenant with Users

### 3.1 API and command shape

- **API**: e.g. `POST /api/tenants` with body containing tenant + list of tenant users.
- **Request body (example)**:

```json
{
  "name": "Acme Corp",
  "slug": "acme",
  "users": [
    {
      "email": "admin@acme.com",
      "displayName": "Admin",
      "password": "secret",
      "isTenantOwner": true
    }
  ]
}
```

- **Application**: One command, e.g. `CreateTenantWithUsersCommand`, containing tenant data + list of user DTOs. One handler orchestrates: create tenant → call Identity for each user → update tenant users with Identity user IDs → save.

### 3.2 Create-tenant-with-users orchestration (uses abstraction)

The handler (or application service) depends on **IIdentityApplicationService** so it works in both Monolith (in-process) and distributed (HTTP) without code changes.

```csharp
// CreateTenantWithUsersCommandHandler (or TenantService) – uses IIdentityApplicationService
public class CreateTenantWithUsersCommandHandler : IRequestHandler<CreateTenantWithUsersCommand, Result<TenantCreatedResult>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IIdentityApplicationService _identityService;  // abstraction: MediatR in Monolith, HTTP in distributed
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTenantWithUsersCommandHandler(
        ITenantRepository tenantRepository,
        IIdentityApplicationService identityService,
        ITenantUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantRepository = tenantRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TenantCreatedResult>> Handle(CreateTenantWithUsersCommand request, CancellationToken cancellationToken)
    {
        // 1. Create tenant in Tenant DB first
        var tenantResult = Tenant.Create(request.Name, request.Slug, _dateTimeProvider);
        if (tenantResult.IsFailure) return Result<TenantCreatedResult>.Failure(tenantResult.Error);
        var tenant = tenantResult.Value;
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUserIds = new List<Guid>();
        try
        {
            // 2. Create each user in Identity (in-process or HTTP depending on topology)
            foreach (var user in request.Users)
            {
                var createReq = new CreateUserRequest(tenant.Id, user.Email, user.DisplayName, user.Password, user.IsTenantOwner);
                var userResult = await _identityService.CreateUserAsync(createReq, cancellationToken);
                if (userResult.IsFailure)
                {
                    await RollbackAsync(tenant.Id, createdUserIds, cancellationToken);
                    return Result<TenantCreatedResult>.Failure(userResult.Error);
                }
                createdUserIds.Add(userResult.Value);
                // 3. Add tenant_user to aggregate and persist
                tenant.AddUser(userResult.Value, user.IsTenantOwner, _dateTimeProvider);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<TenantCreatedResult>.Success(new TenantCreatedResult(tenant.Id, createdUserIds));
        }
        catch (Exception ex)
        {
            await RollbackAsync(tenant.Id, createdUserIds, cancellationToken);
            return Result<TenantCreatedResult>.Failure(Error.Failure("Tenant.Create", ex.Message));
        }
    }

    private async Task RollbackAsync(Guid tenantId, List<Guid> identityUserIds, CancellationToken cancellationToken)
    {
        // Delete tenant (and tenant_users) in Tenant DB
        // Optionally call _identityService.DeleteUserAsync for each identityUserIds
    }
}
```

- **Monolith**: `IIdentityApplicationService` is registered as `IdentityApplicationService` (MediatR). **Distributed**: registered as `IdentityHttpClient` with named HttpClient `"identity"`.

### 3.3 Host registration (topology-based)

- **Monolith**: Register `IIdentityApplicationService` → `IdentityApplicationService` (MediatR). No HttpClient.
- **Distributed (Tenant host only, or host that calls Identity over HTTP)**: Register named HttpClient `"identity"` (base address from service discovery or `Services:Identity:BaseUrl`), then `IIdentityApplicationService` → `IdentityHttpClient`. Add retry/timeout/circuit-breaker on the client.

---

## 4. Identity Service: Endpoint for Tenant Users

### 4.1 Contract

- **Endpoint**: e.g. `POST /api/identity/create-tenant-user` (or reuse `POST /api/identity/users` with a clear contract).
- **Request body (example)**:

```json
{
  "tenantId": "guid",
  "tenantName": "Acme Corp",
  "email": "admin@acme.com",
  "displayName": "Admin",
  "password": "secret",
  "isTenantOwner": true
}
```

- **Response**: 201 Created with body `{ "userId": "guid" }`; or 4xx/5xx with a problem-details or error payload.

### 4.2 Implementation

- Map request to existing `CreateUserCommand` (DefaultTenantId, Email, DisplayName, Password) and send via MediatR (or equivalent).
- Return status and `userId` so Tenant can store `user_id` in `tenant_user` and propagate failures consistently.

---

## 5. Failure Propagation (Distributed / Microservices)

### 5.1 Principles

- **HTTP status codes**: Identity returns 4xx (client error, e.g. validation, conflict) or 5xx (server error). Tenant treats non-success as failure.
- **Structured errors**: Identity should return a consistent error body (e.g. RFC 7807 Problem Details or a simple `{ "code", "message" }`) so Tenant can map to `Result.Failure(Error...)` and return a consistent API error to the client.
- **No distributed 2PC**: Each service commits its own transaction. If Identity fails after Tenant has committed the tenant row, Tenant performs a **compensating action** (delete tenant and optionally tell Identity to delete created users).

### 5.2 Partial failures (e.g. 2 users created, 3rd fails)

**Scenario**: Tenant creates User1 and User2 in Identity successfully; the call to create User3 fails (4xx/5xx/timeout). Tenant has already committed the tenant row and has two Identity user IDs.

**Approach: compensating actions (no distributed transaction coordinator)**  
- Do **not** use a distributed transaction coordinator (2PC). Each service keeps its own transaction boundary.  
- Use **application-level compensation** in the same request: on any Identity failure after at least one success, Tenant (1) rolls back the tenant (delete tenant and tenant_user rows in Tenant DB), and (2) calls Identity `DeleteUserAsync` for each user already created in this request (User1, User2), then returns a single failure to the client.  
- This is an **orchestration-style saga** with compensating actions: the Tenant handler is the orchestrator; there is no separate saga coordinator. No out-of-band saga state store is required for this linear “create tenant + N users” flow.

**Order on partial failure**  
1. Record which Identity user IDs were created (e.g. in a list).  
2. On failure: delete tenant (and tenant_users) in Tenant DB; for each created user ID, call `_identityService.DeleteUserAsync(userId)` (best effort; log if delete fails).  
3. Return `Result.Failure` with a clear message (e.g. “Failed to create user 3 in Identity; rolled back tenant and removed created users.”).

**When a full saga framework might be needed**  
- If the flow becomes long-running (e.g. human approval, multiple services, hours/days), or compensation cannot run in the same HTTP request, consider a saga orchestrator and persisted saga state. For “create tenant with users” in one request, in-request compensation is sufficient.

### 5.3 Failure cases and behavior

| Scenario | Tenant action | Propagate to client |
|----------|----------------|---------------------|
| Identity returns 4xx (e.g. email already exists) | Rollback tenant (and any Identity users already created in this request) | Return 4xx with message/code from Identity (or mapped) |
| Identity returns 5xx or timeout | Retry (if policy says so); then rollback tenant and optionally Identity users | Return 503 or 500 with clear message that Identity was unavailable |
| Identity unreachable (connection failure) | Rollback tenant; log | Return 503 "Identity service unavailable" |
| Tenant DB fails after Identity succeeded | Compensating: call Identity to delete created users (best effort); return 500 | Return 500; client may retry create-tenant |
| Partial success (e.g. 2 users created, 3rd fails) | Compensating: delete tenant and call DeleteUserAsync for each created user (see 5.2) | Return 4xx/5xx with message that creation was rolled back |

### 5.4 Retries and resilience (Tenant → Identity)

- **Retry**: Transient failures (5xx, timeouts) with exponential backoff and max retries (e.g. Polly).
- **Circuit breaker**: After N failures, stop calling Identity for a short period to avoid cascading failures; then allow one call to test recovery.
- **Timeout**: Every call to Identity has a timeout (e.g. 10–30 s) so the Tenant API does not hang.

Implement these on the **named HttpClient** `"identity"` using **Polly** so all calls from Tenant to Identity are protected.

#### 5.4.1 Resilience with Polly (implementation)

**Package**: Add to the Tenant host (or the project that registers the `"identity"` HttpClient):

- `Microsoft.Extensions.Http.Polly` (integrates Polly with `IHttpClientFactory`)

**Policy order**: When using `AddPolicyHandler`, the **last** added policy is the **innermost** (closest to the HTTP call). Typical order: **timeout → retry → circuit breaker** (so circuit breaker wraps retry, retry wraps timeout). Use `AddPolicyHandlerFromRegistry` or explicit ordering if needed; for simplicity, add in order: retry, then circuit breaker (timeout can be set on `HttpClient.Timeout` instead of a Polly policy).

**Retry policy**: Retry on transient failures only (do not retry 4xx except perhaps 408/429). Retry on:
- `HttpRequestException` (e.g. connection failure)
- `TaskCanceledException` / `OperationCanceledException` (timeout)
- HTTP 5xx responses
- HTTP 408 (Request Timeout) or 429 (Too Many Requests) if desired

Use **exponential backoff** (e.g. 2s, 4s, 8s) and cap retries (e.g. 3) so the create-tenant request does not wait too long.

**Circuit breaker**: Open after N consecutive failures (e.g. 5), stay open for a duration (e.g. 30 s), then allow one trial call (half-open). Prevents hammering Identity when it is down.

**Example** (in the Tenant host’s extension or Program.cs):

```csharp
using Polly;
using Polly.Extensions.Http;

// Retry: exponential backoff, max 3 retries; retry on 5xx, timeout, and HttpRequestException
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, attempt, ctx) => { /* log */ });
}

// Circuit breaker: open after 5 failures, break for 30 seconds
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => (int)r.StatusCode >= 500)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) => { /* log */ },
            onReset: () => { /* log */ });
}
```

**Register the named HttpClient with policies**:

```csharp
builder.Services.AddHttpClient("identity", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Services:identity:https"] ?? config["Services:identity:http"] ?? config["Services:Identity:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());
```

- **Do not retry non-idempotent operations** without idempotency: create-user is retried only because Identity’s create-user is defined as idempotent by (tenantId, email) (see §5.5).
- **Timeout**: Prefer `HttpClient.Timeout = TimeSpan.FromSeconds(30)` so each attempt (including retries) is bounded; optionally add a Polly timeout policy if you need a shorter per-attempt limit.

### 5.5 Idempotency (retries and duplicate calls)

**Problem**: If Tenant retries after a timeout or network failure, it may call `CreateUserAsync` again with the same email. Without idempotency, Identity could create a duplicate user or return a conflict on the second call.

**Options**:

1. **Idempotency by business key (recommended minimum)**  
   Identity’s create-user logic treats **(tenantId, email)** as a natural key: if a user with that email already exists for that tenant, return **200 OK** (or 201) with the **existing** user’s ID instead of 409 Conflict. Then Tenant’s retry gets the same ID and can continue safely. No new endpoint contract; just define that “create” is idempotent for (tenantId, email).

2. **Idempotency key header (strong guarantee)**  
   Tenant sends a unique key per logical operation (e.g. `X-Idempotency-Key: <guid>` or request-scoped id). Identity stores the key and the result (e.g. userId); if the same key is sent again within a TTL, Identity returns the **same** response without creating a second user. Requires Identity to persist idempotency keys (e.g. in DB or cache) and to return the cached response for duplicate keys.

**Recommendation**: Implement (1) so that “create user for tenant X with email Y” is idempotent by (tenantId, email). Optionally add (2) for strict once-only semantics under retries and at-least-once delivery.

**Tenant side**: On retry, use the same request (same emails). Identity’s idempotent behavior ensures no duplicate users.

### 5.6 Logging and correlation

- Use a correlation ID (or trace ID) on the request and pass it to Identity (e.g. header `X-Correlation-Id` or `Request-Id`). When Identity logs or returns errors, include that ID so support can correlate Tenant and Identity logs.

---

## 5.7 Aspire service discovery (WithReference, config keys, without Aspire)

### How WithReference(identity) works

- In the AppHost you define a project with a **service name** (e.g. `builder.AddProject("identity", ...)`). When another project is added with `.WithReference(identity)`, Aspire **injects configuration** into that consuming project at runtime.
- The injected configuration contains the **resolvable endpoint URL(s)** of the referenced project (host/port where the Identity host is listening). The consuming project (e.g. Tenant host) can then use that URL as the base address for its HttpClient to Identity.
- Only projects that **explicitly** reference another project receive that project’s discovery data. So the Tenant host gets Identity’s URL only if the AppHost has `tenant.WithReference(identity)`.

### What configuration keys are injected

- Aspire injects entries under **`Services:<serviceName>`**, where `<serviceName>` is the first argument to `AddProject` (e.g. `"identity"`).
- Typical keys (exact names can depend on Aspire version and endpoint names):
  - **`Services:identity:https`** — HTTPS endpoint URL (e.g. `https://localhost:5002` or the resolved host when running in Aspire).
  - **`Services:identity:http`** — HTTP endpoint URL.
  - If the project has **named endpoints** (e.g. `.WithHttpsEndpoint(port: 5002, name: "api")`), you may see **`Services:identity:api`** or similar.
- The Tenant host should read base URL in a defined order, for example:  
  `config["Services:identity:https"] ?? config["Services:identity:http"] ?? config["Services:Identity:BaseUrl"]`  
  so that Aspire-injected keys take precedence and a fallback works when not using Aspire.

### Running without Aspire

- When the Tenant host runs **without** Aspire (e.g. started directly with `dotnet run` or from IIS), **no** service discovery configuration is injected. The `Services:identity:https` / `Services:identity:http` keys will be absent.
- The host must then rely on **explicit configuration**: e.g. **`Services:Identity:BaseUrl`** in `appsettings.json`, `appsettings.Development.json`, or environment variables (e.g. `Services__Identity__BaseUrl`). Use the same key in the HttpClient configuration so that both “with Aspire” and “without Aspire” work: prefer discovery keys first, then fall back to `Services:Identity:BaseUrl`.

---

## 6. AppHost updates

Use `Deployment:Topology` from configuration (e.g. `AppHost/appsettings.json` or launch profile) to switch topologies. Ensure the AppHost has project references to all host projects it adds.

### 6.1 Monolith topology

Single host runs both Tenant and Identity. No cross-project reference for Identity.

```csharp
case "Monolith":
    builder.AddProject("monolith", "../Hosts/MonolithHost/Monolith.Host.csproj")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
        .WithReference(postgres)
        .WithReference(redis)
        .WithReference(rabbitMq);
    break;
```

### 6.2 DistributedApp topology

Multiple hosts (controlpanel, runtime, appbuilder, gateway). If Tenant and Identity run in the **same** host (e.g. ControlPanel loads both), no reference needed. If you **split** so Tenant is in one host and Identity in another (e.g. ControlPanel = Tenant only, Runtime = Identity), give the Tenant host a reference to the Identity host so it receives the Identity URL via service discovery:

```csharp
case "DistributedApp":
    var runtime = builder.AddProject("runtime", "../Hosts/MultiAppRuntimeHost/MultiApp.Runtime.Host.csproj")
        .WithHttpEndpoint(port: 56802, targetPort: 56802, name: "runtimeHttp")
        .WithHttpsEndpoint(port: 56798, targetPort: 56798, name: "runtimeHttps")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
        .WithReference(postgres)
        .WithReference(redis)
        .WithReference(rabbitMq);

    builder.AddProject("controlpanel", "../Hosts/MultiAppControlPanelHost/MultiApp.ControlPanel.Host.csproj")
        .WithHttpEndpoint(port: 8081, targetPort: 81, name: "controlpanelHttp")
        .WithHttpsEndpoint(port: 8444, targetPort: 8444, name: "controlpanelHttps")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
        .WithReference(postgres)
        .WithReference(redis)
        .WithReference(rabbitMq)
        .WithReference(runtime);  // only if ControlPanel calls Identity over HTTP (e.g. LoadedModules = Tenant only and Identity runs in Runtime)

    // ... appbuilder, gateway as today
    break;
```

### 6.3 Microservices topology

Separate Identity and Tenant services. Tenant project gets `WithReference(identity)` so the Tenant host receives the Identity service URL.

```csharp
case "Microservices":
    var identity = builder.AddProject("identity", "../Hosts/IdentityServiceHost/IdentityService.Host.csproj")  // or path to your Identity host
        .WithHttpEndpoint(port: 5002, targetPort: 5002, name: "http")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
        .WithReference(postgres)
        .WithReference(redis)
        .WithReference(rabbitMq);

    builder.AddProject("tenant", "../Hosts/TenantServiceHost/TenantService.Host.csproj")  // or path to your Tenant-only host
        .WithHttpEndpoint(port: 5001, targetPort: 5001, name: "http")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId)
        .WithReference(postgres)
        .WithReference(redis)
        .WithReference(rabbitMq)
        .WithReference(identity);

    builder.AddProject("gateway", "../ApiGateway/ApiGateway.csproj")
        .WithEnvironment("DATARIZEN_ASPIRE_RUN_ID", aspireRunId);
    break;
```

If you do not yet have separate Identity/Tenant host projects, you can keep using the existing Monolith host for Microservices temporarily and add the dedicated projects when ready; then switch the AppHost to the snippet above.

---

## 7. Host project updates

### 7.1 MonolithHost (Monolith topology)

Register both modules and the **in-process** implementation of `IIdentityApplicationService`. No HttpClient for Identity.

```csharp
// Program.cs (existing module registration)
builder.Services.AddModule<TenantModule>(builder.Configuration);
builder.Services.AddModule<IdentityModule>(builder.Configuration);
builder.Services.AddModule<UserModule>(builder.Configuration);
builder.Services.AddModule<FeatureModule>(builder.Configuration);

// Topology-aware: when Monolith, Tenant calls Identity in-process via MediatR
var topology = builder.Configuration["Deployment:Topology"] ?? "Monolith";
if (string.Equals(topology, "Monolith", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IIdentityApplicationService, IdentityApplicationService>();
}
// When running as part of DistributedApp with both modules loaded, same: use in-process
else if (/* both Tenant and Identity are in this host, e.g. LoadedModules contains both */)
{
    builder.Services.AddScoped<IIdentityApplicationService, IdentityApplicationService>();
}
```

Ensure `Identity.Application` (or the assembly that contains `IdentityApplicationService`) is referenced by MonolithHost and that `IdentityApplicationService` sends `CreateUserCommand` via MediatR.

### 7.2 MultiApp hosts (DistributedApp topology)

When the host runs **only Tenant** (or needs to call Identity in another process), register the HTTP-based client and the named HttpClient. When it runs **both** Tenant and Identity, register the in-process implementation (same as Monolith).

**Option A – ControlPanel has both Tenant and Identity (current setup):**  
Register in-process implementation in ControlPanel host (same as MonolithHost snippet above, when LoadedModules includes both TenantManagement and Identity).

**Option B – ControlPanel has only Tenant, Identity is in Runtime:**  
In ControlPanel host Program.cs (or a Tenant-specific extension):

```csharp
// When this host has Tenant but not Identity, call Identity over HTTP
var loadedModules = builder.Configuration.GetSection("LoadedModules").Get<string[]>() ?? Array.Empty<string>();
bool hasIdentity = loadedModules.Contains("Identity");
bool hasTenant = loadedModules.Contains("TenantManagement");

if (hasTenant && !hasIdentity)
{
    builder.Services.AddHttpClient("identity", (sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["Services:runtime:https"] ?? config["Services:runtime:http"] ?? config["Services:Identity:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddScoped<IIdentityApplicationService, IdentityHttpClient>();
}
else if (hasTenant && hasIdentity)
{
    builder.Services.AddScoped<IIdentityApplicationService, IdentityApplicationService>();
}
```

When using Aspire, `WithReference(runtime)` injects keys like `Services:runtime:https` into the ControlPanel project.

### 7.3 Microservices – Tenant host only

The host that runs only the Tenant module must use the HTTP client for Identity and get the base URL from service discovery (Aspire injects it via `WithReference(identity)`).

```csharp
// TenantService.Host (or the project used as "tenant" in AppHost) Program.cs
builder.Services.AddModule<TenantModule>(builder.Configuration);

builder.Services.AddHttpClient("identity", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Services:identity:https"] ?? config["Services:identity:http"] ?? config["Services:Identity:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddScoped<IIdentityApplicationService, IdentityHttpClient>();
```

Add package `Microsoft.Extensions.Http.Polly` and define `GetRetryPolicy()` and `GetCircuitBreakerPolicy()` as in §5.4.1 (Resilience with Polly).

### 7.4 Configuration (appsettings) per host

- **MonolithHost**  
  - `Deployment:Topology`: `"Monolith"`  
  - No `Services:Identity:BaseUrl` needed.

- **MultiApp ControlPanel** (when calling Identity over HTTP)  
  - `Services:Identity:BaseUrl` fallback when not running under Aspire (e.g. `http://localhost:56802`).

- **Microservices Tenant host**  
  - Aspire injects `Services:identity:https` / `Services:identity:http`.  
  - Fallback `Services:Identity:BaseUrl` for local run without AppHost.

---

## 8. Implementation Checklist

### 8.1 Contract and implementations

- [ ] Define `IIdentityApplicationService` (e.g. in Identity.Contracts) with `CreateUserAsync` and `DeleteUserAsync`; add `CreateUserRequest` (and ensure Tenant.Application references the contract).
- [ ] Implement **IdentityApplicationService** (MediatR) in Identity.Application for in-process use (Monolith / same-host).
- [ ] Implement **IdentityHttpClient** (HTTP) for distributed use; use `IHttpClientFactory.CreateClient("identity")` and call `POST /api/identity/create-tenant-user`.

### 8.2 Identity module

- [ ] Add `POST /api/identity/create-tenant-user` (or extend `POST /api/identity/users`) with request/response DTOs.
- [ ] Map to `CreateUserCommand` and return `userId` and appropriate status/errors.
- [ ] Return structured errors (e.g. Problem Details) for 4xx/5xx.
- [ ] **Idempotency**: Make create-user idempotent by (tenantId, email): if user already exists for that tenant+email, return existing userId (see §5.5). Optional: support `X-Idempotency-Key` for strict once-only semantics.
- [ ] Optional: Accept correlation ID header and log it.

### 8.3 Tenant module

- [ ] Extend create-tenant API to accept tenant + list of tenant users (email, display name, password, isTenantOwner).
- [ ] Introduce `CreateTenantWithUsersCommand` and handler that uses **IIdentityApplicationService** (no direct HTTP).
- [ ] Orchestration: create tenant in Tenant DB → for each user call `_identityService.CreateUserAsync` → persist tenant_user; on failure run **compensating actions** (delete tenant and call `DeleteUserAsync` for each already-created user — see §5.2 partial failures).

### 8.4 Monolith host

- [ ] In **MonolithHost** Program.cs: when `Deployment:Topology` is Monolith (or when both Tenant and Identity are loaded), register `IIdentityApplicationService` → `IdentityApplicationService`.

### 8.5 Distributed / Microservices hosts

- [ ] In hosts that run Tenant but not Identity: register named HttpClient `"identity"` (base URL from config or Aspire service discovery), then `IIdentityApplicationService` → `IdentityHttpClient`.
- [ ] **Polly resilience**: Add package `Microsoft.Extensions.Http.Polly`; implement retry policy (transient errors, exponential backoff, max 3 retries) and circuit breaker (e.g. 5 failures, 30 s break); register with `.AddPolicyHandler(GetRetryPolicy()).AddPolicyHandler(GetCircuitBreakerPolicy())` on the `"identity"` HttpClient (see §5.4.1).
- [ ] Set `HttpClient.Timeout` (e.g. 30 s) for the `"identity"` client.
- [ ] Add fallback `Services:Identity:BaseUrl` (and for DistributedApp with Runtime, `Services:runtime:https` or similar) in appsettings where needed.

### 8.6 AppHost

- [ ] **Monolith**: Keep single `monolith` project; no Identity reference.
- [ ] **DistributedApp**: If Tenant and Identity are in different hosts, add `.WithReference(runtime)` (or the Identity host) to the Tenant host project.
- [ ] **Microservices**: Add `identity` and `tenant` projects; call `.WithReference(identity)` on the `tenant` project. Use correct project paths for your solution (e.g. `../Hosts/IdentityServiceHost/...` when those projects exist).

### 8.7 Configuration and service discovery

- [ ] Document topology values: `Monolith`, `DistributedApp`, `Microservices`.
- [ ] Document Aspire-injected keys (`Services:identity:https`, `Services:identity:http`) and fallback `Services:Identity:BaseUrl` when running without Aspire (see §5.7).

---

## 9. Summary

- **Flow**: Client calls Tenant API with tenant + users → Tenant creates tenant in its DB first → Tenant calls Identity (in-process or HTTP) to create each user → Tenant persists tenant_user links → on any failure, Tenant rolls back and optionally compensates Identity.
- **Topologies**: **Monolith** → in-process (`IIdentityApplicationService` = MediatR). **DistributedApp** / **Microservices** → when Identity is in another process, HTTP via named client `"identity"` and `IdentityHttpClient`.
- **AppHost**: Monolith = one project; DistributedApp = optional `WithReference` from Tenant host to Identity host; Microservices = identity + tenant projects with `tenant.WithReference(identity)`.
- **Hosts**: MonolithHost registers in-process implementation; Tenant-only or cross-process hosts register HttpClient `"identity"` and `IdentityHttpClient`, with retry/circuit breaker and base URL from discovery or config.
- **Failures**: Identity returns HTTP status and structured body; Tenant maps to `Result` and client-facing errors, uses retries/timeout/circuit breaker in HTTP case, and implements **compensating actions** for partial failures (no DTC; orchestration-style saga in request). **Idempotency** (e.g. by tenantId+email or idempotency key) avoids duplicate users on retries. **Aspire**: `WithReference(identity)` injects `Services:identity:https`/`http` into the Tenant project; without Aspire, use `Services:Identity:BaseUrl`.
