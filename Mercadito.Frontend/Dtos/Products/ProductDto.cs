namespace Mercadito.Frontend.Dtos.Products;

public sealed record ProductDto(
    long Id,
    string Name,
    string Description,
    int Stock,
    string Batch,
    DateOnly ExpirationDate,
    decimal Price,
    IReadOnlyList<string> Categories);
