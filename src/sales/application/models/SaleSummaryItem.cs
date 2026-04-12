namespace Mercadito.src.sales.application.models
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
