# Datarizen (PoC)

This repository is the proof-of-concept scaffold for **Datarizen**.

## Goal

Create a fully functional project skeleton (server + clients + infrastructure + docs) that is ready for business logic implementation.

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Node.js 20+
- Git

## Repository layout

- `server/` - .NET solution, building blocks, modules, and hosts
- `clients/` - frontend applications (web)
- `infrastructure/` - docker compose, local dev infrastructure, deployment artifacts
- `scripts/` - automation scripts (development, CI/CD helpers)
- `docs/` - architecture, API, deployment, and implementation plans

## Getting started

**One-time setup (Windows):** If you build the server and see Windows Security blocking `Tenant.Migrations.dll`, run the dev code-signing setup once: see [server/certs/README.md](server/certs/README.md).

Implementation steps are tracked in:

- `docs/implementations/scaffold-plan.md`

