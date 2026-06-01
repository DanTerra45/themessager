using ServicioFrontend.Dtos.Categories;

namespace ServicioFrontend.Dtos.Products;

public sealed record ProductPageDto(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CategoryDto> Categories,
    bool HasPreviousPage,
    bool HasNextPage);
