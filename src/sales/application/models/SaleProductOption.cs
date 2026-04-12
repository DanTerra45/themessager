namespace Mercadito.src.sales.application.models
{
    public sealed record SaleProductOption(
        long Id,
        string Name,
        string Batch,
        decimal Price,
        int Stock);
}
