using AppBuilder.Application.DTOs;
using AppBuilder.Application.Queries.GetInstallableApplications;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>Catalog API: applications available for tenants to install (public apps with an active release).</summary>
[ApiController]
[Route("api/appbuilder/catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public CatalogController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>List applications in the tenant install catalog (public, with active release).</summary>
    [HttpGet("applications")]
    [ProducesResponseType(typeof(List<InstallableApplicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetInstallableApplicationsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    private IActionResult ResultToProblem(Result result) =>
        Problem(detail: result.Error.Message, statusCode: result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        });

    private IActionResult ResultToProblem<T>(Result<T> result) =>
        Problem(detail: result.Error.Message, statusCode: result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        });
}
