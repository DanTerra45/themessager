namespace ServicioReportes.Infrastructure.Messaging;

internal sealed record SaleRegisteredEvent(
    long SaleId,
    string Code,
    DateTime CreatedAt,
    long ActorUserId,
    string ActorUsername,
    long CustomerId,
    string CustomerCiNit,
    string CustomerBusinessName,
    string Channel,
    string PaymentMethod,
    decimal Total,
    string AmountInWords,
    IReadOnlyList<SaleRegisteredLineEvent> Lines);

internal sealed record SaleRegisteredLineEvent(
    long ProductId,
    string ProductName,
    string LotCode,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

internal sealed record SaleCancelledEvent(
    long SaleId,
    string Reason,
    long ActorUserId,
    string ActorUsername,
    DateTime CancelledAt);
