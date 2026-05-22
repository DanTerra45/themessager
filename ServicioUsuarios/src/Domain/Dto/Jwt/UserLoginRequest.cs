namespace Domain.Dto.Jwt
{
    public record UserLoginRequest(
        string Email,
        string Password
    ){}
}