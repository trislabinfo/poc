using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Identity.Contracts;
using Identity.Contracts.Services;
using Tenant.Application.DTOs;
using Tenant.Application.Mappers;
using Tenant.Domain.Repositories;
using TenantEntity = Tenant.Domain.Entities.Tenant;

namespace Tenant.Application.Commands.CreateTenantWithUsers;

public sealed class CreateTenantWithUsersCommandHandler
    : IApplicationRequestHandler<CreateTenantWithUsersCommand, Result<TenantWithUsersDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IIdentityApplicationService _identityService;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTenantWithUsersCommandHandler(
        ITenantRepository tenantRepository,
        IIdentityApplicationService identityService,
        ITenantUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantRepository = tenantRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TenantWithUsersDto>> HandleAsync(
        CreateTenantWithUsersCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var exists = await _tenantRepository.SlugExistsAsync(normalizedSlug, cancellationToken);
        if (exists)
            return Result<TenantWithUsersDto>.Failure(
                Error.Conflict("Tenant.SlugAlreadyExists", "A tenant with this slug already exists."));

        var tenantResult = TenantEntity.Create(request.Name, normalizedSlug, _dateTimeProvider);
        if (tenantResult.IsFailure)
            return Result<TenantWithUsersDto>.Failure(tenantResult.Error);

        var tenant = tenantResult.Value;
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUserIds = new List<Guid>();
        try
        {
            foreach (var userData in request.Users)
            {
                var createReq = new CreateUserRequest(
                    tenant.Id,
                    userData.Email,
                    userData.DisplayName,
                    userData.Password,
                    userData.IsTenantOwner);
                var userResult = await _identityService.CreateUserAsync(createReq, cancellationToken);
                if (userResult.IsFailure)
                {
                    await RollbackAsync(tenant, createdUserIds, cancellationToken);
                    return Result<TenantWithUsersDto>.Failure(userResult.Error);
                }

                createdUserIds.Add(userResult.Value);
                var addResult = tenant.AddUser(userResult.Value, userData.IsTenantOwner, _dateTimeProvider);
                if (addResult.IsFailure)
                {
                    await RollbackAsync(tenant, createdUserIds, cancellationToken);
                    return Result<TenantWithUsersDto>.Failure(addResult.Error);
                }
            }

            _tenantRepository.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<TenantWithUsersDto>.Success(TenantMapper.ToTenantWithUsersDto(tenant));
        }
        catch (Exception ex)
        {
            await RollbackAsync(tenant, createdUserIds, cancellationToken);
            return Result<TenantWithUsersDto>.Failure(Error.Failure("Tenant.Create", ex.Message));
        }
    }

    private async Task RollbackAsync(TenantEntity tenant, List<Guid> identityUserIds, CancellationToken cancellationToken)
    {
        foreach (var userId in identityUserIds)
        {
            try
            {
                await _identityService.DeleteUserAsync(userId, cancellationToken);
            }
            catch
            {
                // Log and continue rollback
            }
        }

        _tenantRepository.Delete(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
