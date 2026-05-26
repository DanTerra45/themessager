namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record DailyPaymentMethodSummary(
        string PaymentMethod,
        int SalesCount,
        decimal TotalAmount);
}
