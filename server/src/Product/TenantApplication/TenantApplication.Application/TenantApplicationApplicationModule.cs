using BuildingBlocks.Application.Modules;
using System.Reflection;
using TenantApplication.Application.Commands.InstallApplication;

namespace TenantApplication.Application;

/// <summary>Application-layer metadata for the TenantApplication module (MediatR assembly).</summary>
public sealed class TenantApplicationApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(InstallApplicationCommand).Assembly;
}
