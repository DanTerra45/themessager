namespace Mercadito.src.application.sales.models
{
    public sealed record SaleProductOption(
        long Id,
        string Name,
        string Batch,
        decimal Price,
        int Stock);
}
