using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListDataSourcesByApplication;

public sealed class ListDataSourcesByApplicationQueryHandler
    : IApplicationRequestHandler<ListDataSourcesByApplicationQuery, Result<List<DataSourceDefinitionDto>>>
{
    private readonly IDataSourceDefinitionRepository _repository;

    public ListDataSourcesByApplicationQueryHandler(IDataSourceDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<DataSourceDefinitionDto>>> HandleAsync(
        ListDataSourcesByApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByAppDefinitionIdAsync(request.AppDefinitionId, cancellationToken);
        var dtos = list.Select(DataSourceDefinitionMapper.ToDto).ToList();
        return Result<List<DataSourceDefinitionDto>>.Success(dtos);
    }
}
