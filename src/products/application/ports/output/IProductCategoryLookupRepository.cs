using Mercadito.src.categories.application.models;

namespace Mercadito.src.products.application.ports.output
{
    public interface IProductCategoryLookupRepository
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    }
}
