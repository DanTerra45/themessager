namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed class DailyCashClosingReport
    {
        public DateOnly BusinessDate { get; init; }
        public DateTime GeneratedAt { get; init; }
        public int RegisteredSalesCount { get; init; }
        public int CancelledSalesCount { get; init; }
        public decimal RegisteredAmountTotal { get; init; }
        public decimal CancelledAmountTotal { get; init; }
        public decimal AverageTicket { get; init; }
        public IReadOnlyList<DailyPaymentMethodSummary> PaymentMethods { get; init; } = [];
        public IReadOnlyList<DailyProductSalesSummary> Products { get; init; } = [];
    }
}
