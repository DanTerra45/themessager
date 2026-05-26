namespace Domain.Dto.Jwt
{
    public record UserLoginRequest(
        string EmailOrUsername,
        string Password
    ){}
}