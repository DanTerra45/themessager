namespace ServicioProveedores.Contracts.Suppliers;

public sealed record SupplierPageResponse(
    IReadOnlyList<SupplierResponse> Suppliers,
    string NextSupplierCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FieldHints);
