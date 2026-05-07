namespace Mercadito.Frontend.Dtos.Categories;

public sealed record SaveCategoryRequestDto(
    string Code,
    string Name,
    string Description);
