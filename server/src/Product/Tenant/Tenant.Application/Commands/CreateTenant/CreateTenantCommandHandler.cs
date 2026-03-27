using BuildingBlocks.Application.Handlers;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Tenant.Domain.Repositories;
using TenantEntity = Tenant.Domain.Entities.Tenant;

namespace Tenant.Application.Commands.CreateTenant;

public sealed class CreateTenantCommandHandler
    : BaseCreateCommandHandler<TenantEntity, Guid, CreateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ITenantUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
        : base(tenantRepository, unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<Result<TenantEntity>> CreateEntityAsync(
        CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = command.Slug.Trim().ToLowerInvariant();

        var exists = await _tenantRepository.SlugExistsAsync(normalizedSlug, cancellationToken);
        if (exists)
        {
            return Result<TenantEntity>.Failure(
                Error.Conflict("Tenant.SlugAlreadyExists", "A tenant with this slug already exists."));
        }

        return TenantEntity.Create(
            command.Name,
            normalizedSlug,
            _dateTimeProvider);
    }
}
