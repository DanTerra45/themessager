namespace Mercadito.Sales.Api.Contracts.Suppliers;

public sealed record SupplierPageResponse(
    IReadOnlyList<SupplierResponse> Suppliers,
    string NextSupplierCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FieldHints);
