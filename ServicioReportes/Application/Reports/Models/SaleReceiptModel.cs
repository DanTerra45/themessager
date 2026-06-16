namespace ServicioReportes.Application.Reports.Models;

public sealed record SaleReceiptModel(
    long Id,
    string Code,
    DateTime CreatedAt,
    DateTime GeneratedAt,
    string CustomerCiNit,
    string CustomerBusinessName,
    string CreatedBy,
    decimal Total,
    string AmountInWords,
    IReadOnlyList<SaleReceiptLineModel> Lines);

public sealed record SaleReceiptLineModel(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
