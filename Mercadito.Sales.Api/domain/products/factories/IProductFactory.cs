using Mercadito.src.domain.products.entities;

namespace Mercadito.src.domain.products.factories
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
