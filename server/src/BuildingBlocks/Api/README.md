# BuildingBlocks.Web

ASP.NET Core web utilities and composition helpers shared across hosts and modules.

## Structure

- `Filters/`
  - `ExceptionFilter`
  - `ValidationFilter`
- `Middleware/`
  - `ErrorHandlingMiddleware`
- `Extensions/`
  - `ServiceCollectionExtensions` for registering common web infrastructure

## Dependencies

- `BuildingBlocks.Kernel`
- `BuildingBlocks.Contracts`
- ASP.NET Core (`Microsoft.AspNetCore.App` framework reference)

This project should not contain business logic; it focuses on cross-cutting web concerns such as error handling, validation, and common middleware.

