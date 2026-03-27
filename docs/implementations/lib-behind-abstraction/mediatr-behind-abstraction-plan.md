# MediatR Behind Abstraction – Implementation Plan

## Implementation status

| Item | Status |
|------|--------|
| **IRequestDispatcher** – API/controllers use only this | Done |
| **IApplicationRequest&lt;TResponse&gt;** – request marker | Done |
| **IApplicationRequestHandler&lt;TRequest,TResponse&gt;** – handler contract | Done |
| **IRequestPipelineBehavior&lt;TRequest,TResponse&gt;** – pipeline abstraction in BuildingBlocks.Application | Done |
| **Capabilities.Messaging** – MediatR behind abstraction (envelope, bridge, adapter) | Done |
| **Pipeline behaviors** – Validation, Logging, Performance implement our interface; MediatR adapter composes them | Done |
| **Module transaction behaviors** – Tenant, Identity, AppBuilder implement IRequestPipelineBehavior; each module registers its own | Done |
| **RequestEnvelope** – public; implements MediatR IRequest only in capability | Done |
| **Remove MediatR from Application** – Tenant.Application, Identity.Application, BuildingBlocks base handlers: no IRequest/IRequestHandler | Done |
| **Remove MediatR from Application** – AppBuilder.Application, TenantApplication.Application | Done |
| **Domain events** – IDomainEventDispatcher in BuildingBlocks.Application; MediatRDomainEventDispatcher + DomainEventEnvelope in Capabilities.Messaging; IDomainEvent has no INotification | Done |
| **BuildingBlocks.Application** – legacy MediatR pipeline behaviors removed; MediatR/FluentValidation package refs removed | Done |
| **AddBuildingBlocks** – no longer registers MediatR/validators/behaviors; hosts use AddRequestDispatch from Capabilities.Messaging only | Done |
| **IPipelineRequest** – envelope abstraction; RequestEnvelope implements it in capability | Done |

---

## Purpose

Refactor the solution so that **request/response dispatch is fully behind our own interfaces**, with **no MediatR (or any vendor) types in the public API**. This allows:

- **True decoupling**: Application and API layers depend only on our contracts; they never reference MediatR.
- **Easier switch**: Replacing MediatR with Brighter, a custom mediator, or another library requires changes only in the implementation project and registration, not in controllers, services, or handler signatures.
- **Consistent pattern** with existing abstractions: Hangfire (`IBackgroundJobScheduler`), Sentry (`IErrorTracker`), BCrypt (`IPasswordHasher`).

---

## Decoupling principle

- **Public API (contracts):** Only our types. No `IRequest<T>`, `IRequestHandler<,>`, `IPipelineBehavior<,>`, or `INotification` in BuildingBlocks.Application or Kernel from MediatR.
- **Application layer:** Commands/queries implement our request marker; handlers implement our handler interface. No MediatR references.
- **Implementation:** Lives in a dedicated capability project (`Capabilities.Messages`). It adapts our requests/handlers to the chosen message library (envelope + bridge registration) and implements `IRequestDispatcher` (and optionally `IDomainEventDispatcher`). The specifics (e.g. MediatR, Brighter) stay inside the project.
- **Pipeline:** Validation, logging, performance, and transaction are **implementation-specific**. The capability uses its chosen library pipeline; swapping implementations does not change our abstraction.

---

## Reference Pattern: Hangfire

| Layer | Hangfire | Request dispatch (target) |
|-------|----------|---------------------------|
| **Interface** | `BuildingBlocks.Application` → `IBackgroundJobScheduler` | `BuildingBlocks.Application` → `IRequestDispatcher`, `IApplicationRequest<T>`, `IApplicationRequestHandler<TReq,TRes>` |
| **Implementation** | `Capabilities.BackgroundJobs.Hangfire` → `HangfireBackgroundJobScheduler` | `Capabilities.Messages` → dispatcher implementation + adapter (envelope + bridge handlers) |
| **Default / no-op** | `BuildingBlocks.Infrastructure` → `NullBackgroundJobScheduler` | Not needed |
| **Registration** | Host opts in: `builder.AddHangfireBackgroundJobs()` | Host opts in: e.g. `builder.AddRequestDispatch(assemblies)` (registers message pipeline + `IRequestDispatcher`) |

Handlers and request types are **our types only**; the Messages capability adapts them to the underlying message library internally.

---

## Abstraction design (true decoupling)

### 1. Request and handler contracts (our types only)

**Location:** `BuildingBlocks.Application` (e.g. `RequestDispatch/`). **No reference to any message library** (e.g. MediatR) in this project.

```csharp
namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Marker for a request that returns a response of type <typeparamref name="TResponse"/>.
/// Implemented by all commands and queries. Enables dispatch without depending on MediatR or any vendor.
/// </summary>
public interface IApplicationRequest<out TResponse>
{
}

/// <summary>
/// Handles a request and returns a response. Implemented by command/query handlers.
/// The dispatch implementation (e.g. MediatR, Brighter) discovers and invokes these handlers.
/// </summary>
public interface IApplicationRequestHandler<in TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends a request and returns the response. Abstraction over MediatR, Brighter, or a custom mediator.
/// </summary>
public interface IRequestDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IApplicationRequest<TResponse> request, CancellationToken cancellationToken = default);
}
```

- Commands and queries (e.g. `CreateTenantCommand`, `GetTenantByIdQuery`) implement **only** `IApplicationRequest<Result<T>>` (or `IApplicationRequest<Result>`).
- Handlers implement **only** `IApplicationRequestHandler<TRequest, TResponse>` and contain the existing logic.
- Controllers and in-process services depend **only** on `IRequestDispatcher` and call `SendAsync` with our request types.

### 2. Domain events (decoupled from MediatR)

**Location:** `BuildingBlocks.Kernel.Domain`.

- **Current:** `IDomainEvent : INotification` (MediatR) in Kernel.
- **Target:** Remove `INotification` from Kernel. `IDomainEvent` is our marker only.

```csharp
// BuildingBlocks.Kernel.Domain – no MediatR reference
public interface IDomainEvent
{
}
```

- **IDomainEventDispatcher** (in BuildingBlocks.Application): `Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);`
- **Messages capability implementation:** Wraps `IDomainEvent` in an envelope or registers bridge handlers so that publishing our event triggers the correct handlers. Domain event handler interfaces (if we introduce them) are also our own types; the capability adapts to the underlying library.
- **Call sites:** If the codebase already publishes domain events (e.g. in UnitOfWork or a behavior via `IMediator.Publish`), add a step to identify those call sites and replace them with `IDomainEventDispatcher.PublishAsync`. If there are no such call sites yet, treat `IDomainEventDispatcher` as optional for future use.

### 3. Pipeline abstraction (implemented)

- **BuildingBlocks.Application:** `IRequestPipelineBehavior<TRequest, TResponse>` with `HandleAsync(TRequest request, Func<CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken)`. Behaviors receive the inner request and call `next(ct)` to continue the pipeline.
- **Capabilities.Messaging:** Validation, Logging, and Performance behaviors implement `IRequestPipelineBehavior`. A single `MediatRPipelineBehaviorAdapter` implements MediatR’s `IPipelineBehavior<RequestEnvelope<...>, TResponse>` and composes all registered `IRequestPipelineBehavior` instances. MediatR is used only inside the capability.
- **Modules (Tenant, Identity, AppBuilder):** Each transaction behavior implements `IRequestPipelineBehavior` and is registered by the module’s `AddXxxTransactionBehaviors()`. No MediatR types in the module.
- **Switching implementation:** A different library would implement `IRequestDispatcher` and run the same `IRequestPipelineBehavior` chain (or adapt it); no change to our contracts or to behavior logic.

---

## Current MediatR usage (to refactor)

### Controllers / API layer

- **BuildingBlocks.Web:** `BaseApiController`, `BaseCrudController` — depend on `IMediator`, expose `Mediator` for subclasses.
- **Product modules (Api):** Tenant, Identity, AppBuilder, TenantApplication, AppRuntime.BFF controllers — inject `IMediator`, call `Mediator.Send(...)`.

**Change:** Depend on `IRequestDispatcher`; call `SendAsync(...)` with our request types.

### In-process application services

- **Identity.Application:** `IdentityApplicationService` uses `IMediator` to send `CreateUserCommand` / `DeleteUserCommand`.
- **AppRuntime.BFF:** `BffReleaseSnapshotProvider` uses `IMediator` to send queries.

**Change:** Inject `IRequestDispatcher`, use `SendAsync`.

### Application layer: requests and handlers

- **Tenant.Application, Identity.Application:** Commands/queries implement only `IApplicationRequest<TResponse>`; handlers implement only `IApplicationRequestHandler<TRequest, TResponse>`. Base handlers (`BaseCreateCommandHandler`, `BaseGetByIdQueryHandler`) implement only `IApplicationRequestHandler`. No MediatR references.
- **AppBuilder.Application, TenantApplication.Application:** Still implement `IRequest` and `IRequestHandler` for now; can be removed in a follow-up for full decoupling.
- **BuildingBlocks.Application:** Defines `IRequestPipelineBehavior<,>`; legacy pipeline behaviors (ValidationBehavior, etc. in BuildingBlocks) still use MediatR for the legacy `AddBuildingBlocks(assemblies)` path. The Capabilities.Messaging path uses only our pipeline interface.

**Change (true decoupling):**
- Commands/queries implement **only** `IApplicationRequest<TResponse>` (remove `IRequest<TResponse>`).
- Handlers implement **only** `IApplicationRequestHandler<TRequest, TResponse>` (remove `IRequestHandler<,>`).
- Base handlers (e.g. `BaseCreateCommandHandler`, `BaseGetByIdQueryHandler`) implement our handler interface; move or duplicate in a way that does not depend on MediatR.
- **Remove the message library package reference** (e.g. MediatR) from BuildingBlocks.Application and from all module Application projects. Pipeline behaviors and dispatch registration move into **Capabilities.Messages** (see below).

---

## Adapter strategy (envelope + bridge)

To keep Application and API free of vendor types, the **Messages capability** uses an **envelope** and **bridge handlers**. The specifics (which library is used internally, e.g. MediatR) stay inside the capability project.

1. **Envelope:** For each `IApplicationRequest<TResponse>` sent via `SendAsync`, the implementation wraps it in an internal envelope type that holds the inner request. Only the capability project knows this type; it adapts to the underlying message library (e.g. MediatR `IRequest<TResponse>`).

2. **Bridge registration:** At startup, the capability scans assemblies for types implementing `IApplicationRequestHandler<TRequest, TResponse>` (e.g. via reflection or Scrutor over `handlerAssemblies`). For each discovered handler it registers the handler with DI and registers a bridge with the underlying library so that when the envelope is dispatched, the bridge unwraps it, resolves `IApplicationRequestHandler<TRequest, TResponse>` from DI, and calls `HandleAsync(innerRequest, ct)`.

3. **SendAsync:** The dispatcher implementation’s `SendAsync(ourRequest)` creates the envelope, dispatches it through the underlying pipeline, and returns the result. The bridge delegates to our handler.

4. **Pipeline:** ValidationBehavior, LoggingBehavior, PerformanceBehavior, and module transaction behaviors live in the capability. They run in the underlying library’s pipeline (unwrap the envelope, validate/log/measure/transaction the inner request, then call the next step).

5. **Validators:** FluentValidation validators are for the **inner** request type. The capability’s validation step unwraps the envelope and runs validators for the inner type. Validator registration is done by the capability’s registration extension.

Result: Application layer defines only `IApplicationRequest<T>`, `IApplicationRequestHandler<,>`, and uses `IRequestDispatcher`; the message library (e.g. MediatR) is confined to the capability project and can be replaced by another implementation without touching Application or API.

---

## Implementation Steps

### Phase 1: Add our contracts (no MediatR)

1. **BuildingBlocks.Application**
   - Add `IApplicationRequest<TResponse>`, `IApplicationRequestHandler<TRequest, TResponse>`, `IRequestDispatcher` (see Abstraction design above).
   - Do **not** reference MediatR in this project. If the project currently references MediatR, it will be removed in Phase 2 when handlers/requests are switched to our interfaces.

2. **BuildingBlocks.Kernel**
   - Change `IDomainEvent` to **remove** inheritance from MediatR's `INotification`. It becomes a marker only. Remove the MediatR package reference from Kernel if it exists only for this.
   - (Optional) Add `IDomainEventDispatcher` in BuildingBlocks.Application: `Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);`

### Phase 2: Create Capabilities.Messages and adapter

3. **New project: Capabilities.Messages**
   - References: `BuildingBlocks.Application`, `MediatR`, `FluentValidation`, `Microsoft.Extensions.DependencyInjection`.
   - **RequestEnvelope:** Internal type `RequestEnvelope<TRequest, TResponse> : IRequest<TResponse>` holding the inner `IApplicationRequest<TResponse>`.
   - **MediatRRequestDispatcher:** Implements `IRequestDispatcher`. Constructor takes `IMediator`. `SendAsync(ourRequest)` creates envelope, calls `_mediator.Send(envelope, ct)`, returns result.
   - **Bridge handler:** Generic `BridgeRequestHandler<TRequest, TResponse> : IRequestHandler<RequestEnvelope<TRequest, TResponse>, TResponse>` where `TRequest : IApplicationRequest<TResponse>`. In `Handle` it resolves `IApplicationRequestHandler<TRequest, TResponse>` and calls `HandleAsync(envelope.InnerRequest, ct)`.
   - **Pipeline behaviors:** Move or copy ValidationBehavior, LoggingBehavior, PerformanceBehavior from BuildingBlocks.Application into the capability. Adapt them to work with `RequestEnvelope<TReq, TRes>` (unwrap, run validation/logging/performance on inner request, then `next()`). Module-specific transaction behaviors (e.g. TenantTransactionBehavior) are also registered here and work on the inner request type (e.g. check `ITenantCommand` on inner request).
   - **Registration extension:** `AddRequestDispatch(this IServiceCollection services, params Assembly[] handlerAssemblies)`:
     - Registers MediatR and configures it to use the envelope + bridge (custom registration so MediatR dispatches `RequestEnvelope` to bridge handlers).
     - Discovers and registers all `IApplicationRequestHandler<,>` from handlerAssemblies with DI.
     - Registers bridge handlers with MediatR's subscriber registry for each discovered `IApplicationRequestHandler<TReq, TRes>` (map `RequestEnvelope<TReq, TRes>` to bridge).
     - Calls `AddValidatorsFromAssemblies(handlerAssemblies)` so existing FluentValidation validators (for the inner request types) are used by ValidationBehavior.
     - Registers pipeline behaviors.
     - Registers `IRequestDispatcher` as `MediatRRequestDispatcher`.
   - **IDomainEventDispatcher (optional):** Implement with wrapper/envelope that implements `INotification`; register with MediatR and bridge to our event handlers if any.

4. **Remove MediatR from BuildingBlocks.Application** (do this **after** Phase 3 step 6, when all handlers implement only `IApplicationRequestHandler<,>`, so the solution still builds)
   - Remove MediatR package reference.
   - Remove or move pipeline behavior classes to Capabilities.Messages.
   - Base handler classes (`BaseCreateCommandHandler`, `BaseGetByIdQueryHandler`) must implement `IApplicationRequestHandler<,>` only; remove MediatR base. Refactor so they only implement our interface and contain the same logic.

### Phase 3: Refactor Application layer to our contracts

5. **Commands and queries**
   - For each command/query type: implement `IApplicationRequest<TResponse>` and **remove** `IRequest<TResponse>`.
   - Keep existing marker interfaces (e.g. `ITenantCommand`, `ITransactionalCommand`) as-is.

6. **Handlers**
   - For each handler: implement `IApplicationRequestHandler<TRequest, TResponse>` and **remove** `IRequestHandler<,>`.
   - Base handlers: change to implement only `IApplicationRequestHandler<,>`; keep the same logic.

7. **Module Application projects**
   - Remove MediatR package reference from all Application projects (Tenant, Identity, AppBuilder, TenantApplication, etc.).
   - Module transaction behaviors move to Capabilities.Messages so that no Application project references MediatR.

### Phase 4: Refactor API layer and services

8. **BaseApiController / BaseCrudController**
   - Depend on `IRequestDispatcher` instead of `IMediator`; expose it (e.g. protected property) so derived controllers use it in `HandleGetByIdAsync`, `HandleCreateAsync`, and any other method that currently calls `Mediator.Send`. Replace those calls with `_requestDispatcher.SendAsync`.

9. **All controllers and BFF**
   - Inject `IRequestDispatcher`; use `SendAsync(request, ct)` instead of `Mediator.Send`.

10. **In-process services**
    - **Identity.Application:** `IdentityApplicationService` — inject `IRequestDispatcher`, use `SendAsync`.
    - **AppRuntime.BFF:** `BffReleaseSnapshotProvider` — inject `IRequestDispatcher`, use `SendAsync`.

### Phase 5: Host registration and cleanup

11. **Hosts**
    - When request dispatch is needed, hosts call e.g. `services.AddRequestDispatch(applicationAssemblies)` from Capabilities.Messages. That extension registers MediatR, bridge handlers, pipeline, validators, and `IRequestDispatcher`. **Split:** `AddBuildingBlocks(assemblies)` no longer registers MediatR or validators; it continues to register infrastructure (e.g. health, logging). MediatR and validators are registered only by `AddRequestDispatch(assemblies)` when the MediatR capability is used.

12. **Verify decoupling**
    - No API or Application project (except Capabilities.Messages) references MediatR. All request sending goes through `IRequestDispatcher.SendAsync` with `IApplicationRequest<T>` types.

13. **Documentation**
    - Update `docs/ai-context` as described in the subsection below so that AI context and developers see the abstraction-first model and correct types.

---

## Changes in docs/ai-context

Include these documentation updates in the plan so that `docs/ai-context` stays aligned with the abstraction and implementers have a clear checklist.

| File | Changes |
|------|--------|
| **00-OVERVIEW.md** | Replace "MediatR for CQRS" with "Request dispatch (IRequestDispatcher) for CQRS; default implementation is MediatR in Capabilities.Messages." |
| **01-DEPLOYMENT-STRATEGY.md** | Where it says "In-Process (MediatR)" or "MediatR Commands/Queries", state that in-process communication uses `IRequestDispatcher` (implementation: MediatR or other via capability). |
| **02-SOLUTION-STRUCTURE.md** | "Configure inter-module communication" and Behaviors: mention that request dispatch is via `IRequestDispatcher`; pipeline/behaviors are registered by the dispatch capability (e.g. Capabilities.Messages). |
| **03-BUILDING-BLOCKS.md** | Controller examples: use `IRequestDispatcher` and `SendAsync` instead of `IMediator` and `Send`. |
| **05-MODULES.md** | "Delegate to MediatR" → "Delegate to Application layer via IRequestDispatcher (SendAsync)". AddBuildingBlocks/MediatR registration → "Host calls AddRequestDispatch (or another capability) to register IRequestDispatcher and pipeline." FluentValidation/MediatR pipeline → "Validation runs in the dispatch capability pipeline (e.g. MediatR behaviors)." |
| **05-MODULES-APPLICATION-LAYER.md** | "CQRS pattern with MediatR" → "CQRS with request dispatch (IApplicationRequest, IApplicationRequestHandler, IRequestDispatcher)." "Use MediatR for request/response" → "Implement IApplicationRequest and IApplicationRequestHandler; send via IRequestDispatcher." Remove or replace MediatR package reference in examples; reference BuildingBlocks.Application request-dispatch contracts. |
| **05-MODULES-DOMAIN-LAYER.md** | Domain events: "Inherits from INotification (MediatR)" → "Implements IDomainEvent (our marker); dispatch via IDomainEventDispatcher (implementation in capability)." |
| **06-INTER-MODULE-COMMUNICATION.md** | "MediatR Commands/Queries" → "Request dispatch (IRequestDispatcher) for in-process; default implementation is MediatR." All code samples: `IMediator` → `IRequestDispatcher`, `Send` → `SendAsync`, `IRequest`/`IRequestHandler` → `IApplicationRequest`/`IApplicationRequestHandler`. "Module A implements MediatR client" → "implements in-process dispatch (IRequestDispatcher)." Decision guide: "MediatR" → "IRequestDispatcher (MediatR or other)." |
| **08-SERVER-CODING-CONVENTIONS.md** | Controller examples: inject `IRequestDispatcher`, use `SendAsync`. Package references / Directory.Packages.props examples: note that MediatR is optional (used by Capabilities.Messages); Application projects do not reference MediatR. |
| **09-REQUEST-FLOW.md** | Diagram and text: "MediatR Pipeline" → "Request dispatch pipeline (IRequestDispatcher; e.g. MediatR capability)." Controller: `IMediator` → `IRequestDispatcher`, `Send` → `SendAsync`. Stage 6: "MediatR Pipeline (Behaviors)" → "Dispatch pipeline (behaviors are implementation-specific; MediatR capability registers Validation, Logging, Performance, Transaction)." Handler examples: show `IApplicationRequestHandler<,>` and `HandleAsync`. Add a short note: "Dispatch is behind IRequestDispatcher; default implementation is Capabilities.Messages; switching to another library requires only a new capability and registration." |

**Principle:** Docs should describe the **abstraction** (IRequestDispatcher, IApplicationRequest, IApplicationRequestHandler, IDomainEvent) as the contract. Mention MediatR only as the current default implementation (Capabilities.Messages) and that it can be replaced without changing Application or API code.

---

## File / Project Summary

| Item | Action |
|------|--------|
| `BuildingBlocks.Application` | Add `IApplicationRequest<T>`, `IApplicationRequestHandler<,>`, `IRequestDispatcher` (and optionally `IDomainEventDispatcher`). Remove MediatR reference and pipeline behaviors. |
| `BuildingBlocks.Kernel` | `IDomainEvent`: remove `INotification` inheritance; remove MediatR reference if present. |
| `Capabilities.Messages` (new) | RequestEnvelope, BridgeRequestHandler, MediatRRequestDispatcher, pipeline behaviors, validators registration, `AddRequestDispatch`. |
| All module Application projects | Requests implement `IApplicationRequest<T>` only; handlers implement `IApplicationRequestHandler<,>` only; remove MediatR reference. |
| `BuildingBlocks.Web` | `BaseApiController` / `BaseCrudController`: use `IRequestDispatcher`. |
| All Api controllers and BFF | Inject `IRequestDispatcher`; use `SendAsync`. |
| `Identity.Application`, `AppRuntime.BFF` | `IdentityApplicationService`, `BffReleaseSnapshotProvider`: use `IRequestDispatcher`. |
| Hosts | Call `AddRequestDispatch(assemblies)` instead of registering MediatR in `AddBuildingBlocks`. |
| `docs/ai-context` | Update all files that mention MediatR/IMediator so they describe the abstraction (IRequestDispatcher, IApplicationRequest, IApplicationRequestHandler) and mention MediatR only as default implementation. See **Changes in docs/ai-context** above. |

---

## Testing and Rollout

- **Unit tests:** Controllers and in-process services can be tested with a fake `IRequestDispatcher` (e.g. NSubstitute) that returns predefined `Result<T>`.
- **Integration:** Handlers and pipeline run via the adapter; run existing tests and smoke tests after each phase.
- **Switching implementations:** To use Brighter or a custom mediator, add a new capability project that implements `IRequestDispatcher` (and discovers/invokes `IApplicationRequestHandler<,>`), register it instead of `AddRequestDispatch`. No changes to Application or API.
- **Rollback:** To roll back, re-register MediatR and pipeline in `AddBuildingBlocks` and keep `IRequestDispatcher`→`MediatRRequestDispatcher`; revert `BaseApiController`/`BaseCrudController` and any controller or service that was switched to `IRequestDispatcher` back to `IMediator`. Request/handler types can remain as `IApplicationRequest`/`IApplicationRequestHandler` if the capability still adapts them.

---

## Future improvements (optional, enterprise-grade)

The current plan delivers a solid abstraction and swappable implementation. The items below are **optional follow-ups** to add later if compliance, scale, or observability requirements justify them. They do not change the core abstraction; most are pipeline behaviors or small extensions in the capability.

| Improvement | Description | Where to implement |
|-------------|-------------|--------------------|
| **Request context** | Pass correlation ID, tenant ID, user ID into dispatch so every implementation can log/trace consistently. | Optional `SendAsync(..., RequestContext? context = null)` or ensure pipeline has access to `IHttpContextAccessor` / request context; capability propagates context. |
| **Timeout** | Abort long-running handlers after a configurable duration. | Pipeline behavior in the capability (e.g. Polly timeout). No change to `IRequestDispatcher` signature. |
| **Retry / circuit breaker** | Retry on transient failures; circuit breaker to avoid cascading failures. | Pipeline behavior in the capability (e.g. Polly). Implementation-specific. |
| **Idempotency** | For commands: accept an idempotency key so duplicate requests do not run twice. | Pipeline step or wrapper in the capability; store key + result in cache or DB. Only if exactly-once or duplicate-handling is required. |
| **Metrics** | Count requests by type, latency percentiles, success/failure rate for observability. | Pipeline behavior in the capability (e.g. record to OpenTelemetry or metrics sink). |
| **Explicit CQRS split** | Separate `ICommandDispatcher` and `IQueryDispatcher` for different SLAs, scaling, or policies (e.g. read vs write). | Optional: add two interfaces that the same capability implements; or keep single `IRequestDispatcher` and use markers (e.g. `IApplicationCommand<T>`, `IApplicationQuery<T>`). |
| **Null/validation at boundary** | Reject null or obviously invalid requests in `SendAsync` to fail fast. | In each implementation of `IRequestDispatcher` (e.g. guard in `MediatRRequestDispatcher.SendAsync`). Document in interface XML. |
| **Audit at dispatch** | Log every command (who, when, what) for compliance. | Pipeline behavior in the capability (e.g. extend or add AuditLoggingBehavior). |

**Principle:** Keep the abstraction thin. Add these as capability-level or pipeline behaviors where possible so the contract (`IRequestDispatcher`, `IApplicationRequest`, `IApplicationRequestHandler`) stays stable.

---

## Host and assembly registration audit

Every host that **hosts an API or BFF that uses `IRequestDispatcher`** must:

1. Reference **Capabilities.Messaging**.
2. Call **`AddRequestDispatch(applicationAssemblies)`** after **`AddBuildingBlocks(applicationAssemblies)`**.
3. Pass the **application assembly (or assemblies)** that contain the handlers for the requests used by that host’s controllers/services.

Handlers are discovered by scanning the given assemblies for types implementing **`IApplicationRequestHandler<TRequest, TResponse>`**. Each such type is registered together with a MediatR bridge and pipeline behaviors. If a host does not call `AddRequestDispatch`, or passes assemblies that do not contain the handler for a request, the first call to `SendAsync` for that request will fail (e.g. “No handler registered” or 500).

| Host | APIs / BFF hosted | Application assemblies passed to AddRequestDispatch | Notes |
|------|-------------------|------------------------------------------------------|--------|
| **TenantServiceHost** | Tenant.Api | Tenant.Application | CreateTenant, CreateTenantWithUsers, GetTenantById |
| **IdentityServiceHost** | Identity.Api | Identity.Application | CreateUser, UpdateUser, DeleteUser, GetUserById, create-tenant-user |
| **AppBuilderServiceHost** | AppBuilder.Api | AppBuilder.Application | All AppBuilder CRUD commands/queries |
| **TenantApplicationServiceHost** | TenantApplication.Api | TenantApplication.Application | All TenantApplication commands/queries |
| **RuntimeBFFHost** | AppRuntime.BFF | AppRuntime.Application | RuntimeBffController, BffReleaseSnapshotProvider |
| **AppRuntimeServiceHost** | AppRuntime.Api | AppRuntime.Application | No IRequestDispatcher in Api today; AddRequestDispatch for consistency |
| **MonolithHost** | Tenant, Identity, User, Feature, AppBuilder, TenantApplication | Tenant, Identity, AppBuilder, TenantApplication.Application | All modules in one process |
| **MultiAppControlPanelHost** | Tenant, Identity, User, Feature, TenantApplication | Tenant, Identity, TenantApplication (when LoadedModules) | AddRequestDispatch in same block as AddBuildingBlocks |
| **MultiAppAppBuilderHost** | Tenant, Identity, User, Feature, AppBuilder | Tenant, Identity, AppBuilder (when LoadedModules) | AddRequestDispatch + AddBuildingBlocks when modules loaded |
| **MultiAppRuntimeHost** | Tenant, Identity, User, Feature, AppRuntime (BFF) | Tenant, Identity, AppRuntime (when LoadedModules) | AddRequestDispatch + AddBuildingBlocks when modules loaded |

When adding a new API that uses `IRequestDispatcher`, ensure the host that runs that API includes the corresponding **Application** assembly in the list passed to `AddRequestDispatch`.

**Transaction behaviors:** Capabilities.Messaging does **not** reference any product module. Each module (Tenant, Identity, AppBuilder) owns its transaction behavior and registers it via its Application DI entry point: `AddTenantApplication()` calls `AddTenantTransactionBehaviors()`, and similarly for Identity and AppBuilder. The host calls `AddRequestDispatch(assemblies)` (capability) and then `AddModule<TenantModule>()` etc.; the module’s `AddXxxApplication()` registers that module’s transaction behavior for the request-dispatch pipeline. New modules that need a transaction boundary should implement an envelope-aware behavior (`IPipelineBehavior<RequestEnvelope<TRequest,TResponse>, TResponse>`) and register it in their Application service collection extensions using `RequestHandlerDiscovery.GetRequestResponseTypes(assembly)` from Capabilities.Messaging.

---

## Success Criteria

- **True decoupling:** No API or Application project references MediatR (or any vendor request/handler types). Only Capabilities.Messages does.
- All requests implement `IApplicationRequest<TResponse>`; all handlers implement `IApplicationRequestHandler<TRequest, TResponse>`.
- All request sending goes through `IRequestDispatcher.SendAsync`.
- Replacing MediatR with another library requires only a new capability project and host registration change; no changes to controllers, services, or handler/request types.
- Pattern matches Hangfire: our interfaces in BuildingBlocks.Application, implementation in a dedicated capability, registration at host level.
