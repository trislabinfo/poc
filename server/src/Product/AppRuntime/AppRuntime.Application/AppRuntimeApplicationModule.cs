using AppRuntime.Application.Services;
using BuildingBlocks.Application.Modules;
using System.Reflection;

namespace AppRuntime.Application;

/// <summary>
/// Application-layer metadata for the AppRuntime module.
/// Exposes the assembly for host composition (e.g. microservice host).
/// </summary>
public sealed class AppRuntimeApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(CompatibilityCheckService).Assembly;
}
