namespace Domain.Events;

public sealed record UserCreatedEvent(
    int UserId,
    int CreatorUserId,
    string Username,
    string Email,
    string Role,
    DateTime CreatedAt);

public sealed record UserDisabledEvent(
    int UserId,
    int OperatorUserId,
    string PreviousState,
    string NewState,
    string Reason,
    DateTime ChangedAt);

public sealed record UserPasswordChangedEvent(
    int UserId,
    DateTime ChangedAt);
