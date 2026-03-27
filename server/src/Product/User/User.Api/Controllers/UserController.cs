using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace User.Api.Controllers;

[ApiController]
[Route("api/user")]
public sealed class UserController : ControllerBase
{
    /// <summary>
    /// Basic module health/ping endpoint.
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { module = "User", status = "ok" });
    }

    /// <summary>
    /// Lists users (stub).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(new[]
        {
            new { Id = Guid.NewGuid(), Email = "john@example.com", FirstName = "John", LastName = "Doe" },
            new { Id = Guid.NewGuid(), Email = "jane@example.com", FirstName = "Jane", LastName = "Smith" }
        });
    }

    /// <summary>
    /// Gets a user by id (stub).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetById(Guid id)
    {
        return Ok(new { Id = id, Email = "john@example.com", FirstName = "John", LastName = "Doe" });
    }

    /// <summary>
    /// Creates a user (stub).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Create([FromBody] object request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "User creation not implemented yet" });
    }
}

