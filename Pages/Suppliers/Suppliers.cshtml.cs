using Microsoft.AspNetCore.Mvc;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.domain.shared.validation;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.validation;

namespace Mercadito.Pages.Suppliers
{
    public partial class SuppliersModel : AppPageModel
    {
        private const string SearchTermSessionKey = "Suppliers.SearchTerm";
        private const string SortBySessionKey = "Suppliers.SortBy";
        private const string SortDirectionSessionKey = "Suppliers.SortDirection";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private readonly ILogger<SuppliersModel> _logger;
        private readonly IListingPageStateService _listingPageStateService;
        private readonly IRegisterSupplierUseCase _register;
        private readonly IUpdateSupplierUseCase _update;
        private readonly IDeleteSupplierUseCase _delete;
        private readonly IGetAllSuppliersUseCase _getAll;
        private readonly IGetSupplierByIdUseCase _getById;
        private readonly IGetNextSupplierCodeUseCase _getNextSupplierCode;

        public bool ShowCreateSupplierModal { get; set; }
        public bool ShowEditSupplierModal { get; set; }

        public CreateSupplierDto NewSupplier { get; set; } = new CreateSupplierDto
        {
            Codigo = string.Empty,
            Nombre = string.Empty,
            Direccion = string.Empty,
            Contacto = string.Empty,
            Rubro = string.Empty,
            Telefono = string.Empty
        };

        public UpdateSupplierDto EditSupplier { get; set; } = new UpdateSupplierDto
        {
            Codigo = string.Empty,
            Nombre = string.Empty,
            Direccion = string.Empty,
            Contacto = string.Empty,
            Rubro = string.Empty,
            Telefono = string.Empty
        };

        public string SearchTerm { get; set; } = string.Empty;
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public Dictionary<string, List<string>> FieldHints { get; private set; } = [];

        public IReadOnlyList<SupplierRow> ActiveSuppliers { get; private set; } = [];
        public string NextSupplierCodePreview { get; private set; } = "PRV001";
        public Dictionary<string, List<string>> CreateFieldHints { get; private set; } = [];
        public Dictionary<string, List<string>> EditFieldHints { get; private set; } = [];

        public SuppliersModel(
            ISupplierFormHintsProvider supplierFormHintsProvider,
            IListingPageStateService listingPageStateService,
            ILogger<SuppliersModel> logger,
            IRegisterSupplierUseCase register,
            IUpdateSupplierUseCase update,
            IDeleteSupplierUseCase delete,
            IGetAllSuppliersUseCase getAll,
            IGetSupplierByIdUseCase getById,
            IGetNextSupplierCodeUseCase getNextSupplierCode)
        {
            _listingPageStateService = listingPageStateService;
            _logger = logger;
            _register = register;
            _update = update;
            _delete = delete;
            _getAll = getAll;
            _getById = getById;
            _getNextSupplierCode = getNextSupplierCode;
            FieldHints = ToMutableHintsDictionary(supplierFormHintsProvider.GetHints());
            CreateFieldHints = BuildPrefixedHintsDictionary(nameof(NewSupplier), FieldHints);
            EditFieldHints = BuildPrefixedHintsDictionary(nameof(EditSupplier), FieldHints);
        }

        public async Task OnGetAsync()
        {
            ShowCreateSupplierModal = false;
            ShowEditSupplierModal = false;
            LoadStateFromSession();
            await LoadSuppliersAsync();
            await LoadNextSupplierCodePreviewAsync();
            NewSupplier.Codigo = NextSupplierCodePreview;
        }

        public IActionResult OnPostFilter(string searchTerm = "", string sortBy = "", string sortDirection = "", bool clear = false)
        {
            SearchTerm = ValidationText.NormalizeTrimmed(searchTerm);
            if (clear)
            {
                SearchTerm = string.Empty;
            }

            SortBy = NormalizeSortBy(sortBy);
            SortDirection = NormalizeSortDirection(sortDirection);
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            SortBy = NormalizeSortBy(currentSortBy);
            SortDirection = NormalizeSortDirection(currentSortDirection);

            var nextSort = _listingPageStateService.ToggleSort(SortBy, SortDirection, sortBy, BuildListingOptions());
            SortBy = nextSort.SortBy;
            SortDirection = nextSort.SortDirection;
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
            var normalizedSearchTerm = ValidationText.NormalizeTrimmed(SearchTerm);
            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                suppliers = [.. suppliers.Where(supplier => MatchesSearch(supplier, normalizedSearchTerm))];
            }

            ActiveSuppliers = SortSuppliers(suppliers);
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
            var sessionSearchTerm = string.Empty;
            if (persistedSearchTerm is string persistedValue)
            {
                sessionSearchTerm = persistedValue;
            }

            SearchTerm = ValidationText.NormalizeTrimmed(sessionSearchTerm);
            SortBy = NormalizeSortBy(HttpContext.Session.GetString(SortBySessionKey));
            SortDirection = NormalizeSortDirection(HttpContext.Session.GetString(SortDirectionSessionKey));
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetString(SearchTermSessionKey, ValidationText.NormalizeTrimmed(SearchTerm));
            HttpContext.Session.SetString(SortBySessionKey, NormalizeSortBy(SortBy));
            HttpContext.Session.SetString(SortDirectionSessionKey, NormalizeSortDirection(SortDirection));
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

        public string GetSortIcon(string columnName)
        {
            return _listingPageStateService.GetSortIcon(SortBy, SortDirection, columnName, BuildListingOptions());
        }

        private List<SupplierRow> SortSuppliers(List<SupplierRow> suppliers)
        {
            var orderedSuppliers = SortBy switch
            {
                "code" => OrderSuppliers(suppliers, supplier => supplier.Codigo, supplier => supplier.Id),
                "contact" => OrderSuppliers(suppliers, supplier => supplier.Contacto, supplier => supplier.Id),
                "phone" => OrderSuppliers(suppliers, supplier => supplier.Telefono, supplier => supplier.Id),
                "rubro" => OrderSuppliers(suppliers, supplier => supplier.Rubro, supplier => supplier.Id),
                _ => OrderSuppliers(suppliers, supplier => supplier.RazonSocial, supplier => supplier.Id)
            };

            return [.. orderedSuppliers];
        }

        private IOrderedEnumerable<SupplierRow> OrderSuppliers<TKey>(IEnumerable<SupplierRow> suppliers, Func<SupplierRow, TKey> primaryKeySelector, Func<SupplierRow, long> secondaryKeySelector)
        {
            if (string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                return suppliers.OrderByDescending(primaryKeySelector).ThenByDescending(secondaryKeySelector);
            }

            return suppliers.OrderBy(primaryKeySelector).ThenBy(secondaryKeySelector);
        }

        private static string NormalizeSortBy(string? value)
        {
            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);
            if (normalizedValue == "code"
                || normalizedValue == "name"
                || normalizedValue == "contact"
                || normalizedValue == "phone"
                || normalizedValue == "rubro")
            {
                return normalizedValue;
            }

            return DefaultSortBy;
        }

        private static string NormalizeSortDirection(string? value)
        {
            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);
            if (normalizedValue == "desc")
            {
                return "desc";
            }

            return "asc";
        }

        private static ListingPageStateOptions BuildListingOptions()
        {
            return new ListingPageStateOptions(
                new KeysetListingSessionKeys(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    SortBySessionKey,
                    SortDirectionSessionKey,
                    SearchTermSessionKey),
                DefaultSortBy,
                DefaultSortDirection,
                NormalizeSortBy,
                NormalizeSortDirection,
                ValidationText.NormalizeTrimmed);
        }

        public string BuildTooltipMessage(IEnumerable<string> messages)
        {
            return string.Join(" ", messages.Where(message => !string.IsNullOrWhiteSpace(message)).Select(message => $"• {ValidationText.NormalizeTrimmed(message)}"));
        }

        private static Dictionary<string, List<string>> ToMutableHintsDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>> hints)
        {
            var copy = new Dictionary<string, List<string>>(hints.Count);
            foreach (var hint in hints)
            {
                copy[hint.Key] = [.. hint.Value];
            }

            return copy;
        }

        private static Dictionary<string, List<string>> BuildPrefixedHintsDictionary(string prefix, IReadOnlyDictionary<string, List<string>> hints)
        {
            var copy = new Dictionary<string, List<string>>(hints.Count);
            foreach (var hint in hints)
            {
                copy[string.Concat(prefix, ".", hint.Key)] = [.. hint.Value];
            }

            return copy;
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
