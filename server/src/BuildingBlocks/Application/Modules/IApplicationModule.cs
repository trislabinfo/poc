using System.Reflection;

namespace BuildingBlocks.Application.Modules;

/// <summary>
/// Exposes metadata for an application module so the host can
/// compose cross-cutting concerns (MediatR, validators, behaviors)
/// without modules directly wiring those concerns.
/// </summary>
public interface IApplicationModule
{
    /// <summary>
    /// The assembly that contains this module's Application layer
    /// (commands, queries, handlers, validators, etc.).
    /// </summary>
    Assembly ApplicationAssembly { get; }
}

