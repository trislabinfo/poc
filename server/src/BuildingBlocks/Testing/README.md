# BuildingBlocks.Testing

Shared test utilities and base classes for module and application tests.

## Contents

- **Fakes**
  - `FakeDateTimeProvider` – Implements `IDateTimeProvider` with a fixed or configurable UTC time. Use in command/query handler tests for deterministic domain logic.

- **Base**
  - `BaseUnitTest` – Optional base for unit tests; exposes `DateTimeProvider` (FakeDateTimeProvider) and `FakeDateTime` for advancing time.

## Usage

### Handler test with FakeDateTimeProvider

```csharp
public class CreateTenantCommandHandlerTests : BaseUnitTest
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var repository = Substitute.For<ITenantRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new CreateTenantCommandHandler(repository, unitOfWork, DateTimeProvider);

        var result = await handler.Handle(new CreateTenantCommand("Acme", "acme"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }
}
```

### Advancing time

```csharp
FakeDateTime.SetUtcNow(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
// or
FakeDateTime.Advance(TimeSpan.FromDays(7));
```

## References

- **BuildingBlocks.Kernel** – For `IDateTimeProvider`, `Result`, entities.
- **xUnit** – Test framework.
- **FluentAssertions** – Assertions.
- **NSubstitute** – Mocking.
