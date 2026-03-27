using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetNavigationDefinition;

public sealed class GetNavigationDefinitionQueryHandler
    : IApplicationRequestHandler<GetNavigationDefinitionQuery, Result<NavigationDefinitionDto>>
{
    private readonly INavigationDefinitionRepository _repository;

    public GetNavigationDefinitionQueryHandler(INavigationDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NavigationDefinitionDto>> HandleAsync(
        GetNavigationDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.NavigationId, cancellationToken);
        if (entity == null)
            return Result<NavigationDefinitionDto>.Failure(
                Error.NotFound("AppBuilder.NavigationNotFound", "Navigation definition not found."));
        return Result<NavigationDefinitionDto>.Success(NavigationDefinitionMapper.ToDto(entity));
    }
}
