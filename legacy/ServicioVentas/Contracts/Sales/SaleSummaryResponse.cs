namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record SaleSummaryResponse(
    long Id,
    string Code,
    DateTime CreatedAt,
    string CustomerName,
    string Channel,
    string PaymentMethod,
    decimal Total,
    string Status);
