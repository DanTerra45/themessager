using Mercadito.src.notifications.application.models;

namespace Mercadito.src.notifications.application.ports.output
{
    public interface IEmailOutboxRepository
    {
        Task<IReadOnlyList<EmailOutboxItem>> ReservePendingBatchAsync(int batchSize, DateTime currentUtc, CancellationToken cancellationToken = default);
        Task MarkSentAsync(long outboxId, DateTime sentAtUtc, CancellationToken cancellationToken = default);
        Task MarkForRetryAsync(long outboxId, DateTime nextAttemptAtUtc, string errorMessage, CancellationToken cancellationToken = default);
        Task MarkExhaustedAsync(long outboxId, DateTime failedAtUtc, string errorMessage, CancellationToken cancellationToken = default);
    }
}
