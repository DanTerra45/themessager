using Mercadito.src.application.categories.models;
using Mercadito.src.application.products.models;

namespace Mercadito.Pages.Products
{
    public interface IProductListingPageModel
    {
        IReadOnlyList<ProductWithCategoriesModel> Products { get; }
        IReadOnlyList<CategoryModel> Categories { get; }
        long CategoryFilter { get; }
        int CurrentPage { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
        string SortBy { get; }
        string SortDirection { get; }
        string SearchTerm { get; }
        string OrderPreset { get; }
        string GetSortIcon(string columnName);
    }
}
