namespace Mercadito.src.suppliers.application.validation
{
    public interface ISupplierFormHintsProvider
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> GetHints();
    }
}
