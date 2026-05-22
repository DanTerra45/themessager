namespace Domain.Dto.Jwt
{
    public record JwtUserPayload(
        int UserId,
        string Username,
        string Email,
        string Role
    ){}
}