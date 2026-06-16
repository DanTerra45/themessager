namespace Domain.Events;

public static class StockSagaRoutingKeys
{
    public const string ReserveRequested = "sales.stock.reserve.requested";
    public const string RecoverRequested = "sales.stock.recover.requested";
    public const string ReserveSucceeded = "catalog.stock.reserve.succeeded";
    public const string ReserveFailed = "catalog.stock.reserve.failed";
    public const string RecoverSucceeded = "catalog.stock.recover.succeeded";
    public const string RecoverFailed = "catalog.stock.recover.failed";
}

public sealed record StockSagaCommandEvent(
    long ProductId,
    int Quantity,
    long ActorUserId,
    string ActorUsername,
    long? SaleId,
    string? CorrelationId);

public sealed record StockSagaResultEvent(
    long ProductId,
    int Quantity,
    long ActorUserId,
    string ActorUsername,
    long? SaleId,
    string? CorrelationId,
    bool Success,
    bool IsBusinessFailure,
    string? ErrorMessage);
