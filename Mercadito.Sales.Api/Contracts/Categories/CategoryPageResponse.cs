namespace Mercadito.Sales.Api.Contracts.Categories;

public sealed record CategoryPageResponse(
    IReadOnlyList<CategoryResponse> Categories,
    bool HasPreviousPage,
    bool HasNextPage,
    string NextCategoryCode);
