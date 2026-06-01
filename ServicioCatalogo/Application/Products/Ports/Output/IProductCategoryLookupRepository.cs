using ServicioCatalogo.Application.Categories.Models;

namespace ServicioCatalogo.Application.Products.Ports.Output
{
    public interface IProductCategoryLookupRepository
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    }
}

