namespace Domain.Entities;

public sealed class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpirationAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}