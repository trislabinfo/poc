using AppBuilder.Domain.Repositories;
using BuildingBlocks.Application.Handlers;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Commands.CreateAppDefinition;

public sealed class CreateAppDefinitionCommandHandler
    : BaseCreateCommandHandler<AppDefinition.Domain.Entities.Application.AppDefinition, Guid, CreateAppDefinitionCommand>
{
    private readonly IAppDefinitionRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateAppDefinitionCommandHandler(
        IAppDefinitionRepository repository,
        IAppBuilderUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
        : base(repository, unitOfWork)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<Result<AppDefinition.Domain.Entities.Application.AppDefinition>> CreateEntityAsync(
        CreateAppDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = command.Slug.Trim().ToLowerInvariant();
        var exists = await _repository.SlugExistsAsync(normalizedSlug, cancellationToken);
        if (exists)
        {
            return Result<AppDefinition.Domain.Entities.Application.AppDefinition>.Failure(
                Error.Conflict("AppBuilder.SlugAlreadyExists", "An application definition with this slug already exists."));
        }

        return AppDefinition.Domain.Entities.Application.AppDefinition.Create(
            command.Name,
            command.Description,
            normalizedSlug,
            command.IsPublic,
            _dateTimeProvider);
    }
}
