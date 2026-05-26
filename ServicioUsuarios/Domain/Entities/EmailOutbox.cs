namespace Domain.Entities;

public sealed class EmailOutbox
{
    public int Id { get; set; }
    public string? ToAddress { get; set; }
    public string? ToName { get; set; }
    public string? Subject { get; set; }
    public string? PlainTextBody { get; set; }
    public string? HtmlBody { get; set; }
    public string Status { get; set; } = "Pending";
    public int? Attempts { get; set; }
    public DateTime? NextAttempt { get; set; }
    public DateTime? LastAttempt { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}