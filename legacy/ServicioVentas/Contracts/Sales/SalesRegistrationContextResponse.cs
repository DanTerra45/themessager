namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record SalesRegistrationContextResponse(
    string NextSaleCode,
    IReadOnlyList<CustomerOptionResponse> Customers,
    IReadOnlyList<SaleProductOptionResponse> Products);
