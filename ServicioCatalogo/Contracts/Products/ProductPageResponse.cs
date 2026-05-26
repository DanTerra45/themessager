using ServicioCatalogo.Contracts.Categories;

namespace ServicioCatalogo.Contracts.Products;

public sealed record ProductPageResponse(
    IReadOnlyList<ProductResponse> Products,
    IReadOnlyList<CategoryResponse> Categories,
    bool HasPreviousPage,
    bool HasNextPage);
