using Dapper;
using Mercadito.Users.Api.Infrastructure.Shared.Persistence;
using Mercadito.Users.Api.Application.Notifications.Models;
using Mercadito.Users.Api.Application.Notifications.Ports.Output;

namespace Mercadito.Users.Api.Infrastructure.Notifications.Persistence
{
    public sealed class EmailOutboxRepository(IDbConnectionFactory dbConnectionFactory) : IEmailOutboxRepository
    {
        public async Task<IReadOnlyList<EmailOutboxItem>> ReservePendingBatchAsync(int batchSize, DateTime currentUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            const string selectQuery = @"
                    SELECT
                        o.id AS Id,
                        o.toAddress AS ToAddress,
                        o.toName AS ToName,
                        o.subject AS Subject,
                        o.plainTextBody AS PlainTextBody,
                        o.htmlBody AS HtmlBody,
                        o.attempts AS Attempts
                    FROM email_outbox o
                    WHERE o.status = 'P'
                    AND o.nextAttemptAtUtc <= @CurrentUtc
                    ORDER BY o.id ASC
                    LIMIT @BatchSize
                    FOR UPDATE SKIP LOCKED;";

            var selectCommand = new CommandDefinition(
                selectQuery,
                new
                {
                    CurrentUtc = currentUtc,
                    BatchSize = batchSize
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var rows = (await connection.QueryAsync<EmailOutboxItem>(selectCommand)).ToList();
            if (rows.Count == 0)
            {
                transaction.Commit();
                return rows;
            }

            var outboxIds = rows.Select(row => row.Id).ToArray();

            const string reserveQuery = @"
                    UPDATE email_outbox
                    SET status = 'R',
                        attempts = attempts + 1,
                        lastAttemptAtUtc = @CurrentUtc
                    WHERE id IN @OutboxIds;";

            var reserveCommand = new CommandDefinition(
                reserveQuery,
                new
                {
                    CurrentUtc = currentUtc,
                    OutboxIds = outboxIds
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(reserveCommand);
            transaction.Commit();

            foreach (var row in rows)
            {
                row.Attempts++;
            }

            return rows;
        }

        public async Task MarkSentAsync(long outboxId, DateTime sentAtUtc, CancellationToken cancellationToken = default)
        {
            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
                    UPDATE email_outbox
                    SET status = 'S',
                        sentAtUtc = @SentAtUtc,
                        lastError = NULL
                    WHERE id = @OutboxId;";

            var command = new CommandDefinition(
                query,
                new
                {
                    OutboxId = outboxId,
                    SentAtUtc = sentAtUtc
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        public async Task MarkForRetryAsync(long outboxId, DateTime nextAttemptAtUtc, string errorMessage, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(errorMessage);

            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
                    UPDATE email_outbox
                    SET status = 'P',
                        nextAttemptAtUtc = @NextAttemptAtUtc,
                        lastError = @ErrorMessage
                    WHERE id = @OutboxId;";

            var command = new CommandDefinition(
                query,
                new
                {
                    OutboxId = outboxId,
                    NextAttemptAtUtc = nextAttemptAtUtc,
                    ErrorMessage = TrimErrorMessage(errorMessage)
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        public async Task MarkExhaustedAsync(long outboxId, DateTime failedAtUtc, string errorMessage, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(errorMessage);

            using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            const string query = @"
                    UPDATE email_outbox
                    SET status = 'E',
                        nextAttemptAtUtc = @FailedAtUtc,
                        lastError = @ErrorMessage
                    WHERE id = @OutboxId;";

            var command = new CommandDefinition(
                query,
                new
                {
                    OutboxId = outboxId,
                    FailedAtUtc = failedAtUtc,
                    ErrorMessage = TrimErrorMessage(errorMessage)
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        private static string TrimErrorMessage(string errorMessage)
        {
            const int maximumLength = 1000;
            if (errorMessage.Length <= maximumLength)
            {
                return errorMessage;
            }

            return errorMessage[..maximumLength];
        }
    }
}
