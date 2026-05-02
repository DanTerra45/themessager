using Mercadito.src.application.categories.models;

namespace Mercadito.src.application.products.ports.output
{
    public interface IProductCategoryLookupRepository
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    }
}
