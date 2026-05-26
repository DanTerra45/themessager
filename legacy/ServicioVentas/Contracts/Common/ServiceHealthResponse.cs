namespace Mercadito.Sales.Api.Contracts.Common;

public sealed record ServiceHealthResponse(
    string Service,
    string Status,
    DateTimeOffset CheckedAt);
