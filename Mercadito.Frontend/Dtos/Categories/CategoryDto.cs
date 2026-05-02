namespace Mercadito.Frontend.Dtos.Categories;

public sealed record CategoryDto(
    long Id,
    string Code,
    string Name,
    string Description,
    int ProductCount);
