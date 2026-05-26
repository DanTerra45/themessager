using Application.Utils;

namespace Domain.Dto.Register
{
    public record RegisterPasswordResetTokenDto(
        int UserId,
        string Token,
        DateTime? Expiration = null,
        DateTime? CreatedAt = null
    )
    {
        public string Token { get; init; } = PasswordUtils.HashToken(Token);
        public DateTime? CreatedAt { get; init; } = CreatedAt ?? DateTime.UtcNow;
        public DateTime? Expiration { get; init; } = Expiration ?? DateTime.UtcNow.AddMinutes(30);
    }
}