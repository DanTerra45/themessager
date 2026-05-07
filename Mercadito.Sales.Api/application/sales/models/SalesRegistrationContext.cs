namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed class SalesRegistrationContext
    {
        public string NextSaleCode { get; init; } = string.Empty;
        public IReadOnlyList<CustomerLookupItem> Customers { get; init; } = [];
        public IReadOnlyList<SaleProductOption> Products { get; init; } = [];
    }
}
