namespace Mercadito.src.notifications.infrastructure.options
{
    public sealed class EmailOutboxOptions
    {
        public int BatchSize { get; set; } = 10;
        public int PollIntervalSeconds { get; set; } = 10;
        public int MaxAttempts { get; set; } = 5;
        public int BaseRetryDelaySeconds { get; set; } = 30;
    }
}
