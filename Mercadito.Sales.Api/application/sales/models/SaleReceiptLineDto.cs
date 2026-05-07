namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record SaleReceiptLineDto(
        int Quantity,
        string Description,
        decimal UnitPrice,
        decimal Amount);
}
