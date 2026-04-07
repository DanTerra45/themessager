namespace Mercadito.src.audit.domain.entities
{
    public sealed class AuditEntry
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public AuditAction Action { get; set; }
        public string TableName { get; set; } = string.Empty;
        public long RecordId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? PreviousDataJson { get; set; }
        public string? NewDataJson { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
