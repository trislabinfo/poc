using AppBuilder.Application.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListAppDefinitions;

public sealed record ListAppDefinitionsQuery(string? Status = null, bool? IsPublic = null)
    : IApplicationRequest<Result<List<AppDefinitionDto>>>;
