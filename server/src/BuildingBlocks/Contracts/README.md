# BuildingBlocks.Contracts

Shared contracts used for communication between the backend and external callers, as well as between modules.

## Structure

- `Api/` – API-level contracts:
  - `ApiResponse`
  - `ErrorResponse`
- `Messaging/` – integration and application messaging contracts:
  - `IIntegrationEvent`
  - `ICommand`
  - `IQuery`
- `Pagination/` – reusable paging contracts:
  - `PagedRequest`
  - `PagedResponse`

These contracts must remain free of infrastructure concerns and can be referenced by all modules and hosts.

