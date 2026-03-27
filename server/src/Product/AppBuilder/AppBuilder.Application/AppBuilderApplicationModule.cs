using AppBuilder.Application.Commands.CreateAppDefinition;
using BuildingBlocks.Application.Modules;
using System.Reflection;

namespace AppBuilder.Application;

/// <summary>
/// Application-layer metadata for the AppBuilder module.
/// Exposes the assembly that contains commands, queries, handlers, and validators.
/// </summary>
public sealed class AppBuilderApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(CreateAppDefinitionCommand).Assembly;
}
