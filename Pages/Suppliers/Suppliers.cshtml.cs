using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.validation;
using Mercadito.src.shared.domain.validator;
using System.Globalization;

namespace Mercadito.Pages.Suppliers
{
    public partial class SuppliersModel : PageModel
    {
        private const string SearchTermSessionKey = "Suppliers.SearchTerm";
        private readonly IValidator<CreateSupplierDto, SupplierDto> _createValidator;
        private readonly ILogger<SuppliersModel> _logger;
        private readonly IRegisterSupplierUseCase _register;
        private readonly IUpdateSupplierUseCase _update;
        private readonly IGetAllSuppliersUseCase _getAll;
        private readonly IGetSupplierByIdUseCase _getById;
        private readonly IGetNextSupplierCodeUseCase _getNextSupplierCode;

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

        [BindProperty]
        public string EditTelefono { get; set; } = "";

        public List<string>? EditRazonSocialErrors { get; set; }
        public List<string>? EditCodigoErrors { get; set; }
        public List<string>? EditDireccionErrors { get; set; }
        public List<string>? EditContactoErrors { get; set; }
        public List<string>? EditRubroErrors { get; set; }

        public string SearchTerm { get; set; } = string.Empty;
        public Dictionary<string, List<string>> FieldHints { get; private set; } = new();

        public List<SupplierRow> ActiveSuppliers { get; private set; } = [];
        public string NextSupplierCodePreview { get; private set; } = "PRV001";

        public SuppliersModel(
            IValidator<CreateSupplierDto, SupplierDto> createValidator,
            ILogger<SuppliersModel> logger,
            IRegisterSupplierUseCase register,
            IUpdateSupplierUseCase update,
            IGetAllSuppliersUseCase getAll,
            IGetSupplierByIdUseCase getById,
            IGetNextSupplierCodeUseCase getNextSupplierCode)
        {
            _createValidator = createValidator;
            _logger = logger;
            _register = register;
            _update = update;
            _getAll = getAll;
            _getById = getById;
            _getNextSupplierCode = getNextSupplierCode;

            if (_createValidator is SupplierValidator supplierValidator)
            {
                FieldHints = supplierValidator.hints;
            }
        }

        public async Task OnGetAsync()
        {
            ShowModalOnError = false;
            ActiveModal = "";
            LoadStateFromSession();
            await LoadSuppliersAsync();
            await LoadNextSupplierCodePreviewAsync();
            Codigo = NextSupplierCodePreview;
        }

        public IActionResult OnPostFilter(string searchTerm = "", bool clear = false)
        {
            SearchTerm = clear ? string.Empty : NormalizeSearchTerm(searchTerm);
            SaveStateInSession();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDetailsAsync(long id)
        {
            var result = await _getById.ExecuteAsync(id, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                _logger.LogWarning("No se pudo cargar el proveedor {SupplierId}: {Message}", id, result.ErrorMessage);
                return NotFound(new { message = result.ErrorMessage });
            }

            return new JsonResult(result.Value);
        }

        private async Task LoadSuppliersAsync()
        {
            var result = await _getAll.ExecuteAsync(HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                _logger.LogError("No se pudo cargar el listado de proveedores: {Message}", result.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el listado de proveedores.";
                ActiveSuppliers = [];
                return;
            }

            var suppliers = result.Value.Select(MapToRow).ToList();
            var normalizedSearchTerm = NormalizeSearchTerm(SearchTerm);
            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                suppliers = suppliers
                    .Where(supplier => MatchesSearch(supplier, normalizedSearchTerm))
                    .ToList();
            }

            ActiveSuppliers = suppliers;
        }

        private async Task LoadNextSupplierCodePreviewAsync()
        {
            var result = await _getNextSupplierCode.ExecuteAsync(HttpContext.RequestAborted);
            if (result.IsFailure || string.IsNullOrWhiteSpace(result.Value))
            {
                NextSupplierCodePreview = "PRV001";
                return;
            }

            NextSupplierCodePreview = result.Value;
        }

        private static SupplierRow MapToRow(SupplierDto supplier)
        {
            return new SupplierRow(
                supplier.Id,
                supplier.Codigo,
                supplier.Nombre,
                supplier.Contacto,
                supplier.Telefono,
                supplier.Rubro);
        }

        private void LoadStateFromSession()
        {
            var persistedSearchTerm = HttpContext.Session.GetString(SearchTermSessionKey);
            SearchTerm = NormalizeSearchTerm(persistedSearchTerm is string sessionSearchTerm ? sessionSearchTerm : string.Empty);
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetString(SearchTermSessionKey, NormalizeSearchTerm(SearchTerm));
        }

        private static string NormalizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return string.Empty;
            }

            return searchTerm.Trim();
        }

        private static bool MatchesSearch(SupplierRow supplier, string searchTerm)
        {
            return ContainsIgnoreCase(supplier.Codigo, searchTerm)
                || ContainsIgnoreCase(supplier.RazonSocial, searchTerm)
                || ContainsIgnoreCase(supplier.Contacto, searchTerm)
                || ContainsIgnoreCase(supplier.Telefono, searchTerm)
                || ContainsIgnoreCase(supplier.Rubro, searchTerm);
        }

        private static bool ContainsIgnoreCase(string value, string searchTerm)
        {
            return value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyCreateErrors(IReadOnlyDictionary<string, List<string>> errors)
        {
            if (errors.TryGetValue("Nombre", out var nombreErrors))
            {
                RazonSocialErrors = nombreErrors;
            }

            if (errors.TryGetValue("Codigo", out var codigoErrors))
            {
                CodigoErrors = codigoErrors;
            }

            if (errors.TryGetValue("Direccion", out var direccionErrors))
            {
                DireccionErrors = direccionErrors;
            }

            if (errors.TryGetValue("Contacto", out var contactoErrors))
            {
                ContactoErrors = contactoErrors;
            }

            if (errors.TryGetValue("Rubro", out var rubroErrors))
            {
                RubroErrors = rubroErrors;
            }
        }

        private void ApplyEditErrors(IReadOnlyDictionary<string, List<string>> errors)
        {
            if (errors.TryGetValue("Nombre", out var nombreErrors))
            {
                EditRazonSocialErrors = nombreErrors;
            }

            if (errors.TryGetValue("Codigo", out var codigoErrors))
            {
                EditCodigoErrors = codigoErrors;
            }

            if (errors.TryGetValue("Direccion", out var direccionErrors))
            {
                EditDireccionErrors = direccionErrors;
            }

            if (errors.TryGetValue("Contacto", out var contactoErrors))
            {
                EditContactoErrors = contactoErrors;
            }

            if (errors.TryGetValue("Rubro", out var rubroErrors))
            {
                EditRubroErrors = rubroErrors;
            }
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
