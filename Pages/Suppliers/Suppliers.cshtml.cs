using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Suppliers
{
    public class SuppliersModel : PageModel
    {
        public List<SupplierRow> ActiveSuppliers { get; private set; } = [];

        public void OnGet()
        {
            ActiveSuppliers =
            [
                new SupplierRow("PRV-001", "Distribuidora Norte", "Carlos Paredes", "78901234", "Alimentos secos"),
                new SupplierRow("PRV-002", "Lacteos del Valle", "Mariela Quispe", "71234567", "Lacteos y refrigerados"),
                new SupplierRow("PRV-003", "Aseo Hogar SRL", "Luis Romero", "76543210", "Limpieza y desinfeccion"),
                new SupplierRow("PRV-004", "Panificadora Central", "Diana Rios", "79887766", "Panaderia")
            ];
        }
    }

    public sealed record SupplierRow(
        string Codigo,
        string RazonSocial,
        string Contacto,
        string Telefono,
        string Rubro);
}
