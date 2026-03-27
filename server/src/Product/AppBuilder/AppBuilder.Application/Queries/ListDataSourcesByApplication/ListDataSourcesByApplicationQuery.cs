using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListDataSourcesByApplication;

public sealed record ListDataSourcesByApplicationQuery(Guid AppDefinitionId)
    : IApplicationRequest<Result<List<DataSourceDefinitionDto>>>;
