using BuildingBlocks.Kernel.Domain;

namespace Identity.Domain.Events;

public sealed record RefreshTokenCreatedEvent(
    Guid TokenId,
    Guid UserId,
    DateTime ExpiresAt,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);

// TODO Phase 2: EmailConfirmedEvent
// TODO Phase 2: UserLockedEvent, UserUnlockedEvent
// TODO Phase 2: PasswordChangedEvent, PasswordResetEvent
// TODO Phase 2: FailedLoginRecordedEvent, SuccessfulLoginRecordedEvent
// TODO Phase 3: ConsentGrantedEvent, ConsentRevokedEvent
// TODO Phase 4: UserMarkedForDeletionEvent, UserAnonymizedEvent

