namespace Mercadito.Sales.Api.Contracts.Sales;

public sealed record CustomerOptionResponse(
    long Id,
    string CiNit,
    string BusinessName);
