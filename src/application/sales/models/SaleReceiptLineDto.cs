namespace Mercadito.src.application.sales.models
{
    public sealed record SaleReceiptLineDto(
        int Quantity,
        string Description,
        decimal UnitPrice,
        decimal Amount);
}
