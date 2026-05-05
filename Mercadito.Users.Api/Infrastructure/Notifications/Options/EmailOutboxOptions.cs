namespace Mercadito.Users.Api.Infrastructure.Notifications.Options
{
    public sealed class EmailOutboxOptions
    {
        public int BatchSize { get; set; } = 10;
        public int PollIntervalSeconds { get; set; } = 10;
        public int MaxAttempts { get; set; } = 5;
        public int BaseRetryDelaySeconds { get; set; } = 30;
    }
}
