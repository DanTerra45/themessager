namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record SaleSummaryItem(
        long Id,
        string Code,
        DateTime CreatedAt,
        string CustomerDocumentNumber,
        string CustomerName,
        string Channel,
        string PaymentMethod,
        decimal Total,
        string Status);
}
