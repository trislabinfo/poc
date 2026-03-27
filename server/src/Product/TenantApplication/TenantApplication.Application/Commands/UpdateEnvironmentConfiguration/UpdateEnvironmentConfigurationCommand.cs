using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Commands.UpdateEnvironmentConfiguration;

public sealed record UpdateEnvironmentConfigurationCommand(Guid EnvironmentId, string? ConfigurationJson)
    : IApplicationRequest<Result>, ITransactionalCommand, ITenantApplicationCommand;
