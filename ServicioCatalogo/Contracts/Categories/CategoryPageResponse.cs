namespace ServicioCatalogo.Contracts.Categories;

public sealed record CategoryPageResponse(
    IReadOnlyList<CategoryResponse> Categories,
    bool HasPreviousPage,
    bool HasNextPage,
    string NextCategoryCode);
