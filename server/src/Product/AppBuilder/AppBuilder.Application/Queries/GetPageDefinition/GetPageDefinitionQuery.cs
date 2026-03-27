using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetPageDefinition;

public sealed record GetPageDefinitionQuery(Guid PageId) : IApplicationRequest<Result<PageDefinitionDto>>;
