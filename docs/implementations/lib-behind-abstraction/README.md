# Libraries Behind Abstraction

Plans for refactoring third-party libraries to sit behind our own interfaces (same pattern as Hangfire → `IBackgroundJobScheduler`, Sentry → `IErrorTracker`, BCrypt → `IPasswordHasher`).

- **[mediatr-behind-abstraction-plan.md](mediatr-behind-abstraction-plan.md)** – Refactor MediatR behind `IRequestDispatcher` so controllers and in-process services depend on the abstraction, not on `IMediator`.
