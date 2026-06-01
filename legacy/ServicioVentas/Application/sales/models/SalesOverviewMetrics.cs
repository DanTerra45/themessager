namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record SalesOverviewMetrics(
        int RegisteredSalesCount,
        int CancelledSalesCount,
        decimal RegisteredAmountTotal,
        decimal CancelledAmountTotal,
        int SalesTodayCount,
        decimal SalesTodayTotal,
        decimal AverageTicketToday);
}
