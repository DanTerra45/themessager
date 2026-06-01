namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record SaleReceiptResponse(
    long Id,
    string Code,
    DateTime CreatedAt,
    DateTime GeneratedAt,
    string CustomerCiNit,
    string CustomerBusinessName,
    string CreatedBy,
    decimal Total,
    string AmountInWords,
    IReadOnlyList<SaleReceiptLineResponse> Lines);

public sealed record SaleReceiptLineResponse(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
