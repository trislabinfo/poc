using BuildingBlocks.Application.Modules;
using System.Reflection;
using Tenant.Application.Commands.CreateTenant;

namespace Tenant.Application;

/// <summary>
/// Application-layer metadata for the Tenant module.
/// Exposes the assembly that contains commands, queries, handlers, and validators.
/// </summary>
public sealed class TenantApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(CreateTenantCommand).Assembly;
}

