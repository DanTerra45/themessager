namespace Mercadito.Frontend.Dtos.Sales;

public sealed record SalesMetricsDto(
    int RegisteredSales,
    int CancelledSales,
    decimal RegisteredAmount,
    decimal CancelledAmount,
    int SalesToday,
    decimal SalesTodayAmount,
    decimal AverageTicket);
