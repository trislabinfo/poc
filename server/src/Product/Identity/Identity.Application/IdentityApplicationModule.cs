using BuildingBlocks.Application.Modules;
using Identity.Application.Commands.Users.CreateUser;
using System.Reflection;

namespace Identity.Application;

/// <summary>
/// Application-layer metadata for the Identity module.
/// Exposes the assembly that contains commands, queries, handlers, and validators.
/// </summary>
public sealed class IdentityApplicationModule : IApplicationModule
{
    public Assembly ApplicationAssembly => typeof(CreateUserCommand).Assembly;
}

