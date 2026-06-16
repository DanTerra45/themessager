namespace ServicioCatalogo.Infrastructure.Messaging;

internal static class StockSagaRoutingKeys
{
    public const string ReserveRequested = "sales.stock.reserve.requested";
    public const string RecoverRequested = "sales.stock.recover.requested";
    public const string ReserveRequestedLegacy = "sales.stock.reserved";
    public const string RecoverRequestedLegacy = "sales.stock.recovered";
    public const string ReserveSucceeded = "catalog.stock.reserve.succeeded";
    public const string ReserveFailed = "catalog.stock.reserve.failed";
    public const string RecoverSucceeded = "catalog.stock.recover.succeeded";
    public const string RecoverFailed = "catalog.stock.recover.failed";
}

internal sealed record StockSagaCommandEvent(
    long ProductId,
    int Quantity,
    long ActorUserId,
    string? ActorUsername,
    long? SaleId,
    string? CorrelationId);

internal sealed record StockSagaResultEvent(
    long ProductId,
    int Quantity,
    long ActorUserId,
    string? ActorUsername,
    long? SaleId,
    string? CorrelationId,
    bool Success,
    bool IsBusinessFailure,
    string? ErrorMessage);
