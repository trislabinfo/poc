# BuildingBlocks.Kernel

Core domain and application abstractions shared across all modules.

## Purpose

This project defines the fundamental building blocks of the domain model and application layer, independent of any specific module or infrastructure.

## Structure

- `Domain/` – base types for:
  - `Entity`
  - `ValueObject`
  - `AggregateRoot`
  - `DomainEvent`
- `Application/` – application-layer primitives:
  - `Result`
  - `Error`
  - `PagedList`
  - `ICommand`
  - `IQuery`
- `Exceptions/` – shared exception types:
  - `DomainException`
  - `ValidationException`

## Dependencies

- `MediatR` – used for request/response and notification patterns between layers.

