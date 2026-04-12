namespace Mercadito.src.sales.application.models
{
    public sealed record SaleReceiptLineDto(
        int Quantity,
        string Description,
        decimal UnitPrice,
        decimal Amount);
}
