using Mercadito.Frontend.Dtos.Categories;

namespace Mercadito.Frontend.Dtos.Products;

public sealed record ProductPageDto(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CategoryDto> Categories,
    bool HasPreviousPage,
    bool HasNextPage);
