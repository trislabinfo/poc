namespace User.Web.Models;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName);

