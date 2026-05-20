using Domain.Entities.Enums;

namespace Domain.Dto.Register;

public record CreateUserDto(
    string Username,
    string Email,   
    string Password,
    UserRole Role,
    int CreatorId
){}