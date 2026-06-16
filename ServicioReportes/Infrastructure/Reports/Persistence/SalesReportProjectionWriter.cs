using System.Text.Json;
using Dapper;
using ServicioReportes.Application.Common.Ports;

namespace ServicioReportes.Infrastructure.Reports.Persistence;

public sealed class SalesReportProjectionWriter(IReportsDbConnectionFactory connectionFactory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task UpsertRegisteredSaleAsync(SalesReportingProjection registeredSale, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var headerId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    @"
                    INSERT INTO sale_reports (
                        sale_id, code, business_date, created_at, generated_at, status,
                        customer_id, customer_ci_nit, customer_business_name,
                        operator_id, operator_username, channel, payment_method,
                        total, amount_in_words, cancel_reason,
                        cancelled_by_user_id, cancelled_by_username, cancelled_at,
                        created_by_user_id, created_by_username,
                        updated_by_user_id, updated_by_username, updated_at, report_updated_at)
                    VALUES (
                        @SaleId, @Code, @BusinessDate, @CreatedAt, @GeneratedAt, 'Registrada',
                        @CustomerId, @CustomerCiNit, @CustomerBusinessName,
                        @OperatorId, @OperatorUsername, @Channel, @PaymentMethod,
                        @Total, @AmountInWords, NULL,
                        NULL, NULL, NULL,
                        @CreatedByUserId, @CreatedByUsername,
                        @UpdatedByUserId, @UpdatedByUsername, @UpdatedAt, NOW())
                    ON CONFLICT (sale_id) DO UPDATE
                    SET code = EXCLUDED.code,
                        business_date = EXCLUDED.business_date,
                        created_at = EXCLUDED.created_at,
                        generated_at = EXCLUDED.generated_at,
                        status = 'Registrada',
                        customer_id = EXCLUDED.customer_id,
                        customer_ci_nit = EXCLUDED.customer_ci_nit,
                        customer_business_name = EXCLUDED.customer_business_name,
                        operator_id = EXCLUDED.operator_id,
                        operator_username = EXCLUDED.operator_username,
                        channel = EXCLUDED.channel,
                        payment_method = EXCLUDED.payment_method,
                        total = EXCLUDED.total,
                        amount_in_words = EXCLUDED.amount_in_words,
                        cancel_reason = NULL,
                        cancelled_by_user_id = NULL,
                        cancelled_by_username = NULL,
                        cancelled_at = NULL,
                        created_by_user_id = EXCLUDED.created_by_user_id,
                        created_by_username = EXCLUDED.created_by_username,
                        updated_by_user_id = EXCLUDED.updated_by_user_id,
                        updated_by_username = EXCLUDED.updated_by_username,
                        updated_at = EXCLUDED.updated_at,
                        report_updated_at = NOW()
                    RETURNING id;",
                    registeredSale,
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition("DELETE FROM sale_report_lines WHERE sale_report_id = @HeaderId;", new { HeaderId = headerId }, transaction: transaction, cancellationToken: cancellationToken));

            const string insertLineSql = @"
                INSERT INTO sale_report_lines (sale_report_id, product_id, product_name, lot_code, quantity, unit_price, subtotal)
                VALUES (@HeaderId, @ProductId, @ProductName, @LotCode, @Quantity, @UnitPrice, @Subtotal);";

            foreach (var line in registeredSale.Lines)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertLineSql,
                        new { HeaderId = headerId, line.ProductId, line.ProductName, line.LotCode, line.Quantity, line.UnitPrice, line.Subtotal },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }

            await InsertEventLogAsync(connection, transaction, registeredSale.SaleId, "sales.registered", registeredSale.ActorUserId, registeredSale.ActorUsername, JsonSerializer.Serialize(registeredSale, SerializerOptions), cancellationToken);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task MarkSaleCancelledAsync(SaleCancelledProjection cancelledSale, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    @"
                    UPDATE sale_reports
                    SET status = 'Anulada',
                        cancel_reason = @Reason,
                        cancelled_by_user_id = @ActorUserId,
                        cancelled_by_username = @ActorUsername,
                        cancelled_at = @CancelledAt,
                        updated_by_user_id = @ActorUserId,
                        updated_by_username = @ActorUsername,
                        updated_at = @CancelledAt,
                        report_updated_at = NOW()
                    WHERE sale_id = @SaleId;",
                    cancelledSale,
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (affectedRows == 0)
            {
                throw new InvalidOperationException($"No existe la proyección de la venta {cancelledSale.SaleId} para anularla en reportes.");
            }

            await InsertEventLogAsync(connection, transaction, cancelledSale.SaleId, "sales.cancelled", cancelledSale.ActorUserId, cancelledSale.ActorUsername, JsonSerializer.Serialize(cancelledSale, SerializerOptions), cancellationToken);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static Task InsertEventLogAsync(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, long saleId, string eventType, long actorUserId, string actorUsername, string payload, CancellationToken cancellationToken)
    {
        return connection.ExecuteAsync(
            new CommandDefinition(
                @"
                INSERT INTO sale_report_events (sale_id, event_type, actor_user_id, actor_username, payload)
                VALUES (@SaleId, @EventType, @ActorUserId, @ActorUsername, CAST(@Payload AS jsonb));",
                new { SaleId = saleId, EventType = eventType, ActorUserId = actorUserId, ActorUsername = actorUsername, Payload = payload },
                transaction: transaction,
                cancellationToken: cancellationToken));
    }
}

public sealed record SalesReportingProjection(
    long SaleId,
    string Code,
    DateOnly BusinessDate,
    DateTime CreatedAt,
    DateTime GeneratedAt,
    long CustomerId,
    string CustomerCiNit,
    string CustomerBusinessName,
    long OperatorId,
    string OperatorUsername,
    string Channel,
    string PaymentMethod,
    decimal Total,
    string AmountInWords,
    long ActorUserId,
    string ActorUsername,
    long CreatedByUserId,
    string CreatedByUsername,
    long UpdatedByUserId,
    string UpdatedByUsername,
    DateTime UpdatedAt,
    IReadOnlyList<SalesReportingProjectionLine> Lines);

public sealed record SalesReportingProjectionLine(long ProductId, string ProductName, string LotCode, int Quantity, decimal UnitPrice, decimal Subtotal);
public sealed record SaleCancelledProjection(long SaleId, string Reason, long ActorUserId, string ActorUsername, DateTime CancelledAt);
