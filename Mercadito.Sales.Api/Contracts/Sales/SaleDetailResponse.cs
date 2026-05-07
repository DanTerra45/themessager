namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record SaleDetailResponse(
    long Id,
    string Code,
    DateTime CreatedAt,
    string CustomerCiNit,
    string CustomerBusinessName,
    string Channel,
    string PaymentMethod,
    string Status,
    decimal Total,
    IReadOnlyList<SaleDetailLineResponse> Lines);

public sealed record SaleDetailLineResponse(
    long ProductId,
    string ProductName,
    string LotCode,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
