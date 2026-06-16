namespace ServicioReportes.Application.Reports.Models;

public sealed record SaleSummaryModel(
    long Id,
    string Code,
    DateTime CreatedAt,
    string CustomerName,
    string Channel,
    string PaymentMethod,
    decimal Total,
    string Status);
