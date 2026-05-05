namespace Mercadito.Frontend.Dtos.Categories;

public sealed record CategoryPageDto(
    IReadOnlyList<CategoryDto> Categories,
    bool HasPreviousPage,
    bool HasNextPage,
    string NextCategoryCode);
