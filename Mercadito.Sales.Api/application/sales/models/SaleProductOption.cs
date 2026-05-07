namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record SaleProductOption(
        long Id,
        string Name,
        string Batch,
        decimal Price,
        int Stock);
}
