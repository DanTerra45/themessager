namespace Domain.Entities
{
    public record PasswordResetToken
    (
        int Id,
        int UserId,
        string Token,
        DateTime ExpirationAt,
        DateTime UsedAt,
        DateTime CreatedAt
    ){}
}