using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.validator;
using Mercadito.src.shared.domain.validator;
using Mercadito.src.application.suppliers.use_cases;

namespace Mercadito.Pages.Suppliers
{
    public partial class SuppliersModel : PageModel
    {
        private readonly IValidator<CreateSupplierDto, SupplierDto> _createValidator;
        private readonly IValidator<UpdateSupplierDto, SupplierDto> _updateValidator;
        private readonly ILogger<SuppliersModel> _logger;
        private readonly IRegisterSupplierUseCase _register;

        public bool ShowModalOnError { get; set; }
        public string ActiveModal { get; set; } = "";

        [BindProperty]
        public string RazonSocial { get; set; } = "";

        [BindProperty]
        public string Codigo { get; set; } = "";

        [BindProperty]
        public string Direccion { get; set; } = "";

        [BindProperty]
        public string Contacto { get; set; } = "";

        [BindProperty]
        public string Rubro { get; set; } = "";

        public List<string>? RazonSocialErrors { get; set; }
        public List<string>? CodigoErrors { get; set; }
        public List<string>? DireccionErrors { get; set; }
        public List<string>? ContactoErrors { get; set; }
        public List<string>? RubroErrors { get; set; }

        [BindProperty]
        public long EditId { get; set; }

        [BindProperty]
        public string EditCodigo { get; set; } = "";

        [BindProperty]
        public string EditRazonSocial { get; set; } = "";

        [BindProperty]
        public string EditDireccion { get; set; } = "";

        [BindProperty]
        public string EditContacto { get; set; } = "";

        [BindProperty]
        public string EditRubro { get; set; } = "";

        public List<string>? EditRazonSocialErrors { get; set; }
        public List<string>? EditCodigoErrors { get; set; }
        public List<string>? EditDireccionErrors { get; set; }
        public List<string>? EditContactoErrors { get; set; }
        public List<string>? EditRubroErrors { get; set; }

        public Dictionary<string, List<string>> FieldHints { get; private set; } = new();

        public List<SupplierRow> ActiveSuppliers { get; private set; } = [];

        public SuppliersModel(
            IValidator<CreateSupplierDto, SupplierDto> createValidator,
            IValidator<UpdateSupplierDto, SupplierDto> updateValidator,
            ILogger<SuppliersModel> logger,
            IRegisterSupplierUseCase register)
        {
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
            _register = register;
            
            if (_createValidator is SupplierValidator supplierValidator)
            {
                FieldHints = supplierValidator.hints;
            }
        }

        public void OnGet()
        {
            ShowModalOnError = false;
            ActiveModal = "";
            LoadSuppliersStub();
        } 

        private void LoadSuppliersStub()
        {
            ActiveSuppliers =
            [
                new SupplierRow(1, "PRV-001", "Distribuidora Norte", "Carlos Paredes", "78901234", "Alimentos secos"),
                new SupplierRow(2, "PRV-002", "Lacteos del Valle", "Mariela Quispe", "71234567", "Lacteos y refrigerados"),
                new SupplierRow(3, "PRV-003", "Aseo Hogar SRL", "Luis Romero", "76543210", "Limpieza y desinfeccion"),
                new SupplierRow(4, "PRV-004", "Panificadora Central", "Diana Rios", "79887766", "Panaderia")
            ];
        }

        private async Task SaveSupplierStub(CreateSupplierDto supplier)
        {
            long newId = ActiveSuppliers.Count > 0 ? ActiveSuppliers.Max(s => s.Id) + 1 : 1;
            ActiveSuppliers.Add(new SupplierRow(newId, supplier.Codigo, supplier.Nombre, supplier.Contacto, "79887766", supplier.Rubro));
            
            await Task.CompletedTask;
        }

        private async Task UpdateSupplierStub(UpdateSupplierDto supplier)
        {
            var existing = ActiveSuppliers.FirstOrDefault(s => s.Id == supplier.Id);
            if (existing != null)
            {
                ActiveSuppliers.Remove(existing);
                ActiveSuppliers.Add(new SupplierRow(supplier.Id, supplier.Codigo, supplier.Nombre, supplier.Contacto, existing.Telefono, supplier.Rubro));
            }
            
            await Task.CompletedTask;
        }
    }

    public sealed record SupplierRow(
        long Id,
        string Codigo,
        string RazonSocial,
        string Contacto,
        string Telefono,
        string Rubro);
}