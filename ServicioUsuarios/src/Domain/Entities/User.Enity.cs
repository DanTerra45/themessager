using Domain.Entities.Enums;

namespace Domain.Entities;

public record User(
    int Id,
    string Username,
    string Email,   
    string Password,
    UserRole Role,
    int CreatorId,
    DateTime LastLogin,
    bool NeedPasswordChange,
    UserState State,
    DateTime CreatedAt,
    DateTime UpdatedAt
){}