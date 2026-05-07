using Mercadito.Sales.Api.Contracts.Categories;

namespace Mercadito.Sales.Api.Contracts.Products;

public sealed record ProductPageResponse(
    IReadOnlyList<ProductResponse> Products,
    IReadOnlyList<CategoryResponse> Categories,
    bool HasPreviousPage,
    bool HasNextPage);
