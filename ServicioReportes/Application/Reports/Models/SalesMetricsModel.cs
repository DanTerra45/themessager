namespace ServicioReportes.Application.Reports.Models;

public sealed record SalesMetricsModel(
    int RegisteredSales,
    int CancelledSales,
    decimal RegisteredAmount,
    decimal CancelledAmount,
    int SalesToday,
    decimal SalesTodayAmount,
    decimal AverageTicket);
