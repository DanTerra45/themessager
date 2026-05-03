namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record CancelSaleRequest(
    long SaleId,
    string Reason);
