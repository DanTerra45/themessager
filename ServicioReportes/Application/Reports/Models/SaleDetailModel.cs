namespace ServicioReportes.Application.Reports.Models;

public sealed record SaleDetailModel(
    long Id,
    string Code,
    DateTime CreatedAt,
    string CustomerCiNit,
    string CustomerBusinessName,
    string Channel,
    string PaymentMethod,
    string Status,
    decimal Total,
    IReadOnlyList<SaleDetailLineModel> Lines);

public sealed record SaleDetailLineModel(
    long ProductId,
    string ProductName,
    string LotCode,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);
