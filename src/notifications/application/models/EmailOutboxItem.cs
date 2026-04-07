namespace Mercadito.src.notifications.application.models
{
    public sealed class EmailOutboxItem
    {
        public long Id { get; set; }
        public string ToAddress { get; set; } = string.Empty;
        public string? ToName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string PlainTextBody { get; set; } = string.Empty;
        public string? HtmlBody { get; set; }
        public int Attempts { get; set; }
    }
}
