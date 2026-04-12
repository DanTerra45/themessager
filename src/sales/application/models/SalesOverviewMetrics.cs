namespace Mercadito.src.sales.application.models
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
