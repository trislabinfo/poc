# Capabilities.Authentication

Authentication services based on JWT tokens.

## Structure

- `Jwt/`
  - `JwtTokenGenerator`, `JwtTokenValidator` – creating and validating JWT tokens.
- `Services/`
  - `IAuthenticationService` – high-level authentication operations.

## Dependencies

- `BuildingBlocks.Kernel`
- `Microsoft.AspNetCore.Authentication.JwtBearer` – JWT bearer authentication middleware.

