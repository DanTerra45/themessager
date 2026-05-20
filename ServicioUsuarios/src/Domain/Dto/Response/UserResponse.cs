using Domain.Entities.Enums;

namespace Domain.Dto.Response;

public record UserResponse(
    int Id,
    string Username,
    string Email,   
    UserRole Role,
    bool NeedPasswordChange,
    UserState State
){}