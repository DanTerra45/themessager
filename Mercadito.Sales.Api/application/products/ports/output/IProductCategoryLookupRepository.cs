using Mercadito.Sales.Api.Application.Categories.Models;

namespace Mercadito.Sales.Api.Application.Products.Ports.Output
{
    public interface IProductCategoryLookupRepository
    {
        Task<IReadOnlyList<CategoryModel>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    }
}
