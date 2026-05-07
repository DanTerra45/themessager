namespace Mercadito.Sales.Api.Application.Suppliers.Validation
{
    public interface ISupplierFormHintsProvider
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> GetHints();
    }
}
