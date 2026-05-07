using Mercadito.Sales.Api.Domain.Products.Entities;

namespace Mercadito.Sales.Api.Domain.Products.Factories
{
    public sealed record CreateProductValues(
        string Name,
        string Description,
        int? Stock,
        string Batch,
        DateOnly ExpirationDate,
        decimal? Price,
        IReadOnlyCollection<long> CategoryIds);

    public sealed record UpdateProductValues(
        long Id,
        string Name,
        string Description,
        int? Stock,
        string Batch,
        DateOnly ExpirationDate,
        decimal? Price,
        IReadOnlyCollection<long> CategoryIds);

    public interface IProductFactory
    {
        Product CreateForInsert(CreateProductValues input);
        Product CreateForUpdate(UpdateProductValues input);
    }
}
