namespace Mercadito.Sales.Api.Contracts.Products;

public sealed record ProductResponse(
    long Id,
    string Name,
    string Description,
    int Stock,
    string Batch,
    DateOnly ExpirationDate,
    decimal Price,
    IReadOnlyList<string> Categories);
