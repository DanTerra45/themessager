namespace Mercadito.src.notifications.infrastructure.options
{
    public sealed class SmtpOptions
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 1025;
        public bool UseSsl { get; set; }
        public bool AllowInvalidServerCertificate { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}
