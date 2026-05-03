namespace Mercadito.src.application.sales.models
{
    public sealed record SaleDetailLineDto(
        long ProductId,
        string ProductName,
        string Batch,
        int Stock,
        int Quantity,
        decimal UnitPrice,
        decimal Amount);
}
