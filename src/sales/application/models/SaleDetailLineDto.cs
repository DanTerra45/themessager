namespace Mercadito.src.sales.application.models
{
    public sealed record SaleDetailLineDto(
        long ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal Amount);
}
