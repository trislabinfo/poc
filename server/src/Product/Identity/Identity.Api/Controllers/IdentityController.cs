using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Web.Controllers;
using Identity.Api.Requests;
using Identity.Api.Responses;
using Identity.Application.Commands.Users.CreateUser;
using Identity.Application.Commands.Users.DeleteUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : BaseApiController
{
    public IdentityController(IRequestDispatcher requestDispatcher)
        : base(requestDispatcher)
    {
    }
    /// <summary>
    /// Basic module health/ping endpoint.
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { module = "Identity", status = "ok" });
    }

    /// <summary>
    /// Logs in and returns a fake JWT (stub).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Login([FromBody] object credentials)
    {
        return Ok(new
        {
            Token = "fake-jwt-token-12345",
            ExpiresIn = 3600,
            User = new { Id = Guid.NewGuid(), Email = "user@example.com" }
        });
    }

    /// <summary>
    /// Registers a new user (stub).
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Register([FromBody] object request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "Registration not implemented yet" });
    }

    /// <summary>
    /// Refreshes an auth token (stub).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RefreshToken([FromBody] object request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "Token refresh not implemented yet" });
    }

    /// <summary>
    /// Creates a user for a tenant (called by Tenant service when creating tenant with users).
    /// Idempotent by (tenantId, email): if user already exists for that tenant and email, returns existing user id.
    /// </summary>
    [HttpPost("create-tenant-user")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenantUser(
        [FromBody] CreateTenantUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.TenantId,
            request.Email,
            request.DisplayName,
            request.Password ?? string.Empty);
        var result = await RequestDispatcher.SendAsync(command, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(result);
        return CreatedAtAction(
            nameof(CreateTenantUser),
            new { userId = result.Value },
            new CreateTenantUserResponse(result.Value));
    }

    /// <summary>
    /// Deletes a user (e.g. for compensating actions when tenant creation rolls back).
    /// </summary>
    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await RequestDispatcher.SendAsync(new DeleteUserCommand(userId), cancellationToken);
        return ToActionResult(result);
    }
}

