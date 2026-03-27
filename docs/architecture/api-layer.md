# API Layer

## Purpose

The API layer exists to keep HTTP concerns **separate** from module startup/registration and from application/domain logic.

In Datarizen, each module has a dedicated `{Module}.Api` project that contains:

- ASP.NET Core controllers
- API-specific filters and helpers
- Request/response mapping (when needed)

## Where it lives

For a module named `{ModuleName}`:

```
/server/src/Product/{ModuleName}/{ModuleName}.Api
  /Controllers
  /Filters
```

## Guidelines

- **Thin controllers**: no business logic in controllers; delegate to Application (typically via commands/queries).
- **Explicit module routing**: prefer an `api/{module}` prefix to make module boundaries obvious.
- **Shared patterns**: prefer shared filters/middleware from BuildingBlocks when possible; keep module-specific behavior in `{Module}.Api`.

