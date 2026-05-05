namespace Mercadito.Frontend.Dtos.Sales;

public sealed record SaleDetailDto(
    long Id,
    string Code,
    DateTime CreatedAt,
    string CustomerCiNit,
    string CustomerBusinessName,
    string Channel,
    string PaymentMethod,
    string Status,
    decimal Total,
    IReadOnlyList<SaleDetailLineDto> Lines);

public sealed record SaleDetailLineDto(
    long ProductId,
    string ProductName,
    string LotCode,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
