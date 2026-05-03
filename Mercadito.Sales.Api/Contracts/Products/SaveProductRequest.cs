namespace Mercadito.Sales.Api.Contracts.Products;

public sealed record SaveProductRequest(
    string Name,
    string Description,
    int? Stock,
    string Batch,
    DateOnly ExpirationDate,
    decimal? Price,
    IReadOnlyList<long> CategoryIds);
