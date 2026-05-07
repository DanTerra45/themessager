namespace Mercadito.Sales.Api.Application.Sales.Models
{
    public sealed record CustomerLookupItem(
        long Id,
        string DocumentNumber,
        string BusinessName);
}
