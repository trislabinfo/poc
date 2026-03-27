using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Web.Controllers;

/// <summary>
/// Base API controller providing request dispatch and Result-to-IActionResult mapping.
/// All responses use the <see cref="ApiResponse"/> / <see cref="ApiResponse{T}"/> envelope.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Request dispatcher for commands and queries.
    /// </summary>
    protected IRequestDispatcher RequestDispatcher { get; }

    /// <summary>
    /// Initializes the controller with the request dispatcher.
    /// </summary>
    protected BaseApiController(IRequestDispatcher requestDispatcher)
    {
        RequestDispatcher = requestDispatcher;
    }

    /// <summary>
    /// Maps a successful or failed Result to IActionResult with <see cref="ApiResponse{T}"/> envelope.
    /// Success → 200 OK with Data; Failure → status by Error.Type with Error in envelope.
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(ApiResponse<T>.CreateSuccess(result.Value));

        return ToErrorActionResult(result.Error);
    }

    /// <summary>
    /// Maps a successful or failed Result (no value) to IActionResult with <see cref="ApiResponse"/> envelope.
    /// Success → 200 OK with Success=true; Failure → status by Error.Type with Error in envelope.
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return Ok(ApiResponse.CreateSuccess());

        return ToErrorActionResult(result.Error);
    }

    /// <summary>
    /// Returns 201 Created with Location header and <see cref="ApiResponse{T}"/> body.
    /// </summary>
    protected IActionResult CreatedAtGetById<T>(string actionName, object routeValues, T value)
    {
        return CreatedAtAction(actionName, routeValues, ApiResponse<T>.CreateSuccess(value));
    }

    private IActionResult ToErrorActionResult(Error error)
    {
        var errorResponse = new ErrorResponse(
            error.Code,
            error.Message,
            HttpContext.Items["CorrelationId"]?.ToString(),
            DateTime.UtcNow);

        var envelope = ApiResponse.CreateFailure(errorResponse);

        return error.Type switch
        {
            ErrorType.Validation => BadRequest(envelope),
            ErrorType.NotFound => NotFound(envelope),
            ErrorType.Conflict => Conflict(envelope),
            ErrorType.Unauthorized => Unauthorized(envelope),
            ErrorType.Forbidden => StatusCode(403, envelope),
            _ => StatusCode(500, envelope)
        };
    }
}
