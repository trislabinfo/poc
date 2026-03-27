using AppDefinition.Application.Mappers;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetDataSourceDefinition;

public sealed class GetDataSourceDefinitionQueryHandler
    : IApplicationRequestHandler<GetDataSourceDefinitionQuery, Result<DataSourceDefinitionDto>>
{
    private readonly IDataSourceDefinitionRepository _repository;

    public GetDataSourceDefinitionQueryHandler(IDataSourceDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DataSourceDefinitionDto>> HandleAsync(
        GetDataSourceDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.DataSourceId, cancellationToken);
        if (entity == null)
            return Result<DataSourceDefinitionDto>.Failure(
                Error.NotFound("AppBuilder.DataSourceNotFound", "Data source definition not found."));
        return Result<DataSourceDefinitionDto>.Success(DataSourceDefinitionMapper.ToDto(entity));
    }
}
