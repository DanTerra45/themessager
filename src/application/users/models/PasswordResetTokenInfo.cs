namespace Mercadito.src.application.users.models
{
    public sealed class PasswordResetTokenInfo
    {
        public long TokenId { get; set; }
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
