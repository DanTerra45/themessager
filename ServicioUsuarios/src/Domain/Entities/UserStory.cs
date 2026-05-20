using Domain.Entities.Enums;

namespace Domain.Entities;

public record UserStory(
    int Id,
    int UserId,
    int OperatorId,
    UserState PreviousState,
    UserState ActualState,
    string DisableReason,
    DateTime CreatedAt
){}