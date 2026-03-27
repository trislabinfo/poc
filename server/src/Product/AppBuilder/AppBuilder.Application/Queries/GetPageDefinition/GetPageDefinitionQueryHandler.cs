using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetPageDefinition;

public sealed class GetPageDefinitionQueryHandler
    : IApplicationRequestHandler<GetPageDefinitionQuery, Result<PageDefinitionDto>>
{
    private readonly IPageDefinitionRepository _repository;

    public GetPageDefinitionQueryHandler(IPageDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PageDefinitionDto>> HandleAsync(
        GetPageDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.PageId, cancellationToken);
        if (entity == null)
            return Result<PageDefinitionDto>.Failure(
                Error.NotFound("AppBuilder.PageNotFound", "Page definition not found."));
        return Result<PageDefinitionDto>.Success(PageDefinitionMapper.ToDto(entity));
    }
}
