using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Feature.Api.Controllers;

[ApiController]
[Route("api/feature")]
public sealed class FeatureController : ControllerBase
{
    /// <summary>
    /// Basic module health/ping endpoint.
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { module = "Feature", status = "ok" });
    }

    /// <summary>
    /// Lists feature flags (stub).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(new[]
        {
            new { Id = Guid.NewGuid(), Name = "DarkMode", IsEnabled = true, Description = "Dark mode UI" },
            new { Id = Guid.NewGuid(), Name = "BetaFeatures", IsEnabled = false, Description = "Beta features access" }
        });
    }

    /// <summary>
    /// Gets a feature flag by id (stub).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetById(Guid id)
    {
        return Ok(new { Id = id, Name = "DarkMode", IsEnabled = true, Description = "Dark mode UI" });
    }

    /// <summary>
    /// Toggles a feature flag (stub).
    /// </summary>
    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult ToggleFeature(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "Feature toggle not implemented yet", id });
    }
}

