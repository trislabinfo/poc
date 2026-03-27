# ApiGateway

YARP-based reverse proxy for multi-app and microservices topologies.

## Purpose

- Single entry point for API clients when running **DistributedApp** or **Microservices**.
- Path-based routing to backend hosts:
  - **Control panel** (TenantManagement, Identity): `/api/tenant/*`, `/api/identity/*`
  - **Runtime** (UserManagement): `/api/user/*`, `/health`
  - **App builder** (FeatureManagement): `/api/feature/*`

## Configuration

- `appsettings.json`: `ReverseProxy` section with Routes and Clusters.
- Destinations use hostnames `controlpanel`, `runtime`, `appbuilder` so that under Aspire (with service discovery) they resolve to the correct backends.

## Run

Standalone (backends must be running and addresses overridden if needed):

```bash
dotnet run --project server/src/ApiGateway
```

Or run via AppHost with `Deployment:Topology=DistributedApp`; the gateway is started together with the three app hosts.

## Middleware

- CORS: default policy allows any origin/method/header (tighten in production).
- Authentication/Authorization: middleware is registered; add concrete auth (e.g. JWT) when implementing.
- YARP: `MapReverseProxy()` handles forwarding.
