namespace ServicioCatalogo.Infrastructure.Messaging;

internal sealed record SaleStockReservedEvent(
    long ProductId,
    int Quantity,
    long ActorUserId,
    long? SaleId,
    string? CorrelationId);

internal sealed record SaleStockRecoveredEvent(
    long ProductId,
    int Quantity,
    long ActorUserId,
    long? SaleId,
    string? CorrelationId);