namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record DailyProductSalesSummary(
        long ProductId,
        string ProductName,
        int QuantitySold,
        decimal AverageUnitPrice,
        decimal TotalAmount);
}
