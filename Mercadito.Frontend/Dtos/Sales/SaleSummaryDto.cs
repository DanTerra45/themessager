namespace Mercadito.Frontend.Dtos.Sales;

public sealed record SaleSummaryDto(
    long Id,
    string Code,
    DateTime CreatedAt,
    string CustomerName,
    string Channel,
    string PaymentMethod,
    decimal Total,
    string Status);
