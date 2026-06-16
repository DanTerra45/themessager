using System.Collections.Concurrent;
using Application.Sagas;
using Domain.Common;
using Domain.Events;

namespace Infrastructure.Messaging;

public sealed class StockSagaCoordinator(
    IEventPublisher eventPublisher,
    IConfiguration configuration,
    ILogger<StockSagaCoordinator> logger) : IStockSagaCoordinator
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<StockSagaCompletion>> _pending = new(StringComparer.Ordinal);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("RabbitMQ:SagaTimeoutSeconds", 15), 5, 60));

    public Task<Result> ReserveStockAsync(
        long productId,
        int quantity,
        long actorUserId,
        string actorUsername,
        long? saleId,
        CancellationToken cancellationToken = default) =>
        PublishAndAwaitAsync(
            StockSagaRoutingKeys.ReserveRequested,
            new StockSagaCommandEvent(productId, quantity, actorUserId, actorUsername, saleId, null),
            "Lines",
            "StockReserveRejected",
            "No se pudo reservar stock para uno de los productos de la venta.",
            "StockReserveTimeout",
            "La reserva de stock no respondió a tiempo.",
            cancellationToken);

    public Task<Result> RecoverStockAsync(
        long productId,
        int quantity,
        long actorUserId,
        string actorUsername,
        long? saleId,
        CancellationToken cancellationToken = default) =>
        PublishAndAwaitAsync(
            StockSagaRoutingKeys.RecoverRequested,
            new StockSagaCommandEvent(productId, quantity, actorUserId, actorUsername, saleId, null),
            "saleId",
            "StockRecoverRejected",
            "No se pudo recuperar stock para compensar la operación.",
            "StockRecoverTimeout",
            "La recuperación de stock no respondió a tiempo.",
            cancellationToken);

    public bool TryComplete(StockSagaCompletion completion)
    {
        if (_pending.TryGetValue(completion.CorrelationId, out var pending))
        {
            return pending.TrySetResult(completion);
        }

        return false;
    }

    private async Task<Result> PublishAndAwaitAsync(
        string routingKey,
        StockSagaCommandEvent command,
        string errorField,
        string businessErrorCode,
        string defaultBusinessMessage,
        string timeoutErrorCode,
        string timeoutMessage,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var pending = new TaskCompletionSource<StockSagaCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(correlationId, pending))
        {
            return Result.Internal("StockSagaStateError", "No se pudo preparar el estado interno de la saga de stock.");
        }

        try
        {
            await eventPublisher.PublishAsync(
                routingKey,
                command with { CorrelationId = correlationId },
                correlationId);
        }
        catch (Exception exception)
        {
            _pending.TryRemove(correlationId, out _);
            logger.LogError(exception, "No se pudo publicar el evento {RoutingKey} de la saga de stock.", routingKey);
            return Result.Internal("StockSagaPublishFailed", "No se pudo publicar la solicitud de stock.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);
            var completion = await pending.Task.WaitAsync(timeoutCts.Token);
            if (completion.Success)
            {
                return Result.Success();
            }

            if (completion.IsBusinessFailure)
            {
                return Result.Validation(
                    errorField,
                    businessErrorCode,
                    string.IsNullOrWhiteSpace(completion.ErrorMessage) ? defaultBusinessMessage : completion.ErrorMessage!);
            }

            return Result.Internal(
                businessErrorCode,
                string.IsNullOrWhiteSpace(completion.ErrorMessage) ? defaultBusinessMessage : completion.ErrorMessage!);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "La saga de stock {RoutingKey} agotó el tiempo de espera para la correlación {CorrelationId}.",
                routingKey,
                correlationId);
            return Result.Internal(timeoutErrorCode, timeoutMessage);
        }
        finally
        {
            _pending.TryRemove(correlationId, out _);
        }
    }
}
