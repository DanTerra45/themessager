using ServicioFrontend.Dtos.Categories;
using ServicioFrontend.Dtos.Products;

namespace ServicioFrontend.Pages.Products;

public interface IProductListingPageModel
{
    IReadOnlyList<ProductDto> Products { get; }
    IReadOnlyList<CategoryDto> Categories { get; }
    int PageSize { get; }
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
