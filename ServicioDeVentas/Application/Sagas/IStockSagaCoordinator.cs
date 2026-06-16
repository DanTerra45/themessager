using Domain.Common;

namespace Application.Sagas;

public interface IStockSagaCoordinator
{
    Task<Result> ReserveStockAsync(
        long productId,
        int quantity,
        long actorUserId,
        string actorUsername,
        long? saleId,
        CancellationToken cancellationToken = default);

    Task<Result> RecoverStockAsync(
        long productId,
        int quantity,
        long actorUserId,
        string actorUsername,
        long? saleId,
        CancellationToken cancellationToken = default);

    bool TryComplete(StockSagaCompletion completion);
}

public sealed record StockSagaCompletion(
    string CorrelationId,
    string RoutingKey,
    bool Success,
    bool IsBusinessFailure,
    string? ErrorMessage);
