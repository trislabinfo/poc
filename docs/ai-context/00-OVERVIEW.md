# Datarizen AI Context - Overview

## System Identity

You are building **Datarizen**, an enterprise no-code platform that enables business application creation through AI chat agents and visual builders.

## Core Purpose

Datarizen allows users to:
1. Design applications by chatting with AI agents
2. Manually configure applications using visual builders
3. Deploy applications with flexible architecture (monolith → multi-app → microservices)
4. Manage multi-tenant SaaS or single-tenant on-premise deployments

## Deployment Models

### Monolith (Single Application)
All modules deployed together in one application instance:
```
┌─────────────────────────────────┐
│  Single Application             │
│  ┌─────────────────────────┐   │
│  │ All Modules:            │   │
│  │ - Module 1              │   │
│  │ - Module 2              │   │
│  │ - Module 3              │   │
│  │ - Module 4              │   │
│  │ - Module 5              │   │
│  │ - Module N              │   │
│  └─────────────────────────┘   │
└─────────────────────────────────┘
```

### Multi-App (Module Grouping)
Modules split into 2+ applications, each containing one or more modules:
```
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  App 1           │  │  App 2           │  │  App 3           │
│  ┌────────────┐  │  │  ┌────────────┐  │  │  ┌────────────┐  │
│  │ Modules:   │  │  │  │ Modules:   │  │  │  │ Modules:   │  │
│  │ - Module 1 │  │  │  │ - Module 3 │  │  │  │ - Module 5 │  │
│  │ - Module 2 │  │  │  │ - Module 4 │  │  │  │ - Module 6 │  │
│  └────────────┘  │  │  └────────────┘  │  │  └────────────┘  │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

### Microservices (Module per Service)
Each module deployed as independent service:
```
┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
│ Module  │ │ Module  │ │ Module  │ │ Module  │ │ Module  │ │ Module  │
│    1    │ │    2    │ │    3    │ │    4    │ │    5    │ │    N    │
└─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
```

## Tenancy Models

### Multi-Tenant SaaS
- Single deployment serves multiple customers (tenants)
- SaaS administrators manage tenant provisioning, billing, limits
- Data isolation per tenant using TenantId
- Shared infrastructure, isolated data
- Tenant resolution via subdomain, header, or JWT claim

### Single-Tenant On-Premise
- Dedicated deployment for one customer on their infrastructure
- Customer manages their own instance
- No tenant isolation needed (single tenant context)
- Full control over data and infrastructure
- Can still use same codebase with tenant features disabled

## Technology Foundation

### Backend
- **.NET 10** with C# 13
- **ASP.NET Core** for APIs
- **Entity Framework Core** for data access
- **MediatR** for CQRS pattern
- **FluentValidation** for validation rules

### AI Integration
- **Azure OpenAI** or compatible LLM providers
- **Semantic Kernel** for agent orchestration
- **Vector databases** for semantic search

### Data Storage
- **PostgreSQL** or **SQL Server** for relational data
- **Redis** for caching and sessions
- **Blob Storage** for files and assets

### Deployment
- **Docker** containers
- **Kubernetes** or **Azure Container Apps**
- **.NET Aspire** for orchestration (optional)

## Coding Standards

### Naming Conventions
- **PascalCase** for classes, methods, properties, public members
- **camelCase** for local variables, parameters, private fields
- **IPascalCase** for interfaces (prefix with I)
- Descriptive names that reveal intent

### File Organization
- One class per file
- File name matches class name
- Group related files in folders by feature/aggregate

### Design Patterns to Use
- **Repository Pattern** for data access
- **Unit of Work** for transaction management
- **CQRS** for separating reads and writes
- **Mediator** for decoupled request handling
- **Factory** for complex object creation
- **Strategy** for deployment model selection
- **Builder** for complex object construction