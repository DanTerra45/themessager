namespace Mercadito.Users.Api.Domain.Audit.Entities
{
    public sealed class AuditActor
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
