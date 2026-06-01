namespace Domain.Dto.Email;

public sealed record MailMessage(
    string ToAddress,
    string? ToName,
    string Subject,
    string PlainTextBody,
    string? HtmlBody = null
);