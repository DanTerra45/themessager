using Domain.Entities.Enums;

namespace Domain.Dto.Register{
    public record CreateUserDto(
        string Username,
        string Email,   
        string Password,
        string Role
    ){
        
    }
    public record RegisterUserDto(
        string Username,
        string Email,   
        string Password,
        string Role,
        int CreatorId
    ){

    }
}

