namespace ServicioCatalogo.Contracts.Categories;

public sealed record CategoryResponse(
    long Id,
    string Code,
    string Name,
    string Description,
    int ProductCount);
