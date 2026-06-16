namespace ServicioVentas.Contracts.Sales;

public sealed record SaleProductOptionResponse(
    long ProductId,
    string ProductName,
    string LotCode,
    int AvailableStock,
    decimal UnitPrice);
