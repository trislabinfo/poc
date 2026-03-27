using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetDataSourceDefinition;

public sealed record GetDataSourceDefinitionQuery(Guid DataSourceId) : IApplicationRequest<Result<DataSourceDefinitionDto>>;
