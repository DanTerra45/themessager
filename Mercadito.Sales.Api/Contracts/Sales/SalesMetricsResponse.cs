namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record SalesMetricsResponse(
    int RegisteredSales,
    int CancelledSales,
    decimal RegisteredAmount,
    decimal CancelledAmount,
    int SalesToday,
    decimal SalesTodayAmount,
    decimal AverageTicket);
