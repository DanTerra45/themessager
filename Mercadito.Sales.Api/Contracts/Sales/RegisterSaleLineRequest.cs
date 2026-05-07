namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record RegisterSaleLineRequest(
    long ProductId,
    string LotCode,
    int Quantity);
