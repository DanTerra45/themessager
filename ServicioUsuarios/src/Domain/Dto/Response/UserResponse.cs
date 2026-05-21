using Domain.Entities.Enums;

namespace Domain.Dto.Response;

public record UserResponse(
    int Id,
    string Username,
    string Email,   
    string Role,
    bool NeedPasswordChange,
    string State
){}