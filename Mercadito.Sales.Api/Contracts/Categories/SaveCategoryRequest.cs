namespace Mercadito.Sales.Api.Contracts.Categories;

public sealed record SaveCategoryRequest(
    string Code,
    string Name,
    string Description);
