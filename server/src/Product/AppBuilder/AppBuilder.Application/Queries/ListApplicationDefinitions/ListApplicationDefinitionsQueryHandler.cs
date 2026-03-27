using AppBuilder.Application.DTOs;
using AppBuilder.Application.Mappers;
using AppBuilder.Domain.Repositories;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListAppDefinitions;

public sealed class ListAppDefinitionsQueryHandler
    : IApplicationRequestHandler<ListAppDefinitionsQuery, Result<List<AppDefinitionDto>>>
{
    private readonly IAppDefinitionRepository _repository;

    public ListAppDefinitionsQueryHandler(IAppDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<AppDefinitionDto>>> HandleAsync(
        ListAppDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.ListAsync(cancellationToken);
        if (request.Status != null && Enum.TryParse<AppBuilder.Domain.Enums.ApplicationStatus>(request.Status, ignoreCase: true, out var status))
            list = list.Where(x => x.Status == status).ToList();
        if (request.IsPublic.HasValue)
            list = list.Where(x => x.IsPublic == request.IsPublic.Value).ToList();
        var dtos = list.Select(AppDefinitionMapper.ToDto).ToList();
        return Result<List<AppDefinitionDto>>.Success(dtos);
    }
}
