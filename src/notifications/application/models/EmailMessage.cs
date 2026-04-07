namespace Mercadito.src.notifications.application.models
{
    public sealed class EmailMessage
    {
        public string ToAddress { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string PlainTextBody { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
    }
}
