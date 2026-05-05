namespace Mercadito.Frontend.Dtos.Sales;

public sealed record SaleReceiptDto(
    long Id,
    string Code,
    DateTime CreatedAt,
    DateTime GeneratedAt,
    string CustomerCiNit,
    string CustomerBusinessName,
    string CreatedBy,
    decimal Total,
    string AmountInWords,
    IReadOnlyList<SaleReceiptLineDto> Lines);

public sealed record SaleReceiptLineDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
