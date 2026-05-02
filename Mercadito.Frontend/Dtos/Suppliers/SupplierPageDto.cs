namespace Mercadito.Frontend.Dtos.Suppliers;

public sealed record SupplierPageDto(
    IReadOnlyList<SupplierDto> Suppliers,
    string NextSupplierCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FieldHints);
