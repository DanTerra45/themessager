namespace Mercadito.Frontend.Dtos.Sales;

public sealed record SaleProductOptionDto(
    long ProductId,
    string ProductName,
    string LotCode,
    int AvailableStock,
    decimal UnitPrice);
