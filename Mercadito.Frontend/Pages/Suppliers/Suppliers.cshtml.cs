using Mercadito.Frontend.Adapters.Suppliers;
using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Suppliers;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Suppliers;

public sealed class SuppliersModel(
    ISuppliersApiAdapter suppliersApiAdapter,
    ILogger<SuppliersModel> logger) : FrontendPageModel
{
    private const string DefaultSortBy = "name";
    private const string DefaultSortDirection = "asc";

    public bool ShowCreateSupplierModal { get; private set; }
    public bool ShowEditSupplierModal { get; private set; }
    public IReadOnlyList<SupplierRow> ActiveSuppliers { get; private set; } = [];
    public string NextSupplierCodePreview { get; private set; } = "PRV001";
    public Dictionary<string, List<string>> FieldHints { get; private set; } = [];
    public Dictionary<string, List<string>> CreateFieldHints { get; private set; } = [];
    public Dictionary<string, List<string>> EditFieldHints { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = DefaultSortDirection;

    [BindProperty]
    public SupplierFormDto NewSupplier { get; set; } = new();

    [BindProperty]
    public SupplierFormDto EditSupplier { get; set; } = new();

    public async Task OnGetAsync()
    {
        NormalizeState();
        await LoadSuppliersAsync();
        EnsureNewSupplierCode();
    }

    public IActionResult OnPostFilter(
        string searchTerm = "",
        string sortBy = "",
        string sortDirection = "",
        bool clear = false)
    {
        return RedirectToPage(new
        {
            SearchTerm = clear ? string.Empty : NormalizeText(searchTerm),
            SortBy = NormalizeSortBy(sortBy),
            SortDirection = NormalizeSortDirection(sortDirection)
        });
    }

    public IActionResult OnPostSort(
        string sortBy = "",
        string currentSortBy = "",
        string currentSortDirection = "",
        string searchTerm = "")
    {
        var normalizedCurrentSortBy = NormalizeSortBy(currentSortBy);
        var normalizedCurrentDirection = NormalizeSortDirection(currentSortDirection);
        var normalizedRequestedSortBy = NormalizeSortBy(sortBy);
        var nextDirection = "asc";

        if (string.Equals(normalizedCurrentSortBy, normalizedRequestedSortBy, StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedCurrentDirection, "asc", StringComparison.OrdinalIgnoreCase))
        {
            nextDirection = "desc";
        }

        return RedirectToPage(new
        {
            SearchTerm = NormalizeText(searchTerm),
            SortBy = normalizedRequestedSortBy,
            SortDirection = nextDirection
        });
    }

    public async Task<IActionResult> OnGetDetailsAsync(long id)
    {
        var result = await suppliersApiAdapter.GetSupplierAsync(id, HttpContext.RequestAborted);
        if (!result.Success || result.Data == null)
        {
            logger.LogWarning(
                "No se pudo cargar el proveedor {SupplierId}: {Errors}",
                id,
                string.Join(" | ", result.Errors));

            return NotFound(new { message = FirstErrorOrDefault(result, "Proveedor no encontrado.") });
        }

        return new JsonResult(result.Data);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        NormalizeState();
        EnsureNewSupplierCode();
        RemoveModelStateForPrefix(nameof(EditSupplier));
        ModelState.Remove(string.Concat(nameof(NewSupplier), ".", nameof(NewSupplier.Codigo)));

        if (!IsModelStateValidForPrefix(nameof(NewSupplier)))
        {
            LogInvalidModelState(logger, "Suppliers.Create");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
            ShowCreateSupplierModal = true;
            await LoadSuppliersAsync();
            EnsureNewSupplierCode();
            return Page();
        }

        var result = await suppliersApiAdapter.CreateSupplierAsync(
            ToSaveRequest(NewSupplier),
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Proveedor registrado exitosamente.";
            return RedirectToCurrentState();
        }

        ApplyApiErrors(result, nameof(NewSupplier));
        TempData["ErrorMessage"] = FirstErrorOrDefault(result, "Corrige los errores en el formulario.");
        ShowCreateSupplierModal = true;
        await LoadSuppliersAsync();
        EnsureNewSupplierCode();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        NormalizeState();
        RemoveModelStateForPrefix(nameof(NewSupplier));

        if (!IsModelStateValidForPrefix(nameof(EditSupplier)))
        {
            LogInvalidModelState(logger, "Suppliers.Edit");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
            ShowEditSupplierModal = true;
            await LoadSuppliersAsync();
            return Page();
        }

        var result = await suppliersApiAdapter.UpdateSupplierAsync(
            EditSupplier.Id,
            ToSaveRequest(EditSupplier),
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
            return RedirectToCurrentState();
        }

        ApplyApiErrors(result, nameof(EditSupplier));
        TempData["ErrorMessage"] = FirstErrorOrDefault(result, "Corrige los errores en el formulario.");
        ShowEditSupplierModal = true;
        await LoadSuppliersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        NormalizeState();
        var result = await suppliersApiAdapter.DeleteSupplierAsync(
            id,
            BuildActorContext(),
            HttpContext.RequestAborted);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Proveedor desactivado."
            : FirstErrorOrDefault(result, "No se pudo eliminar el proveedor.");

        return RedirectToCurrentState();
    }

    public string GetSortIcon(string columnName)
    {
        if (!string.Equals(SortBy, NormalizeSortBy(columnName), StringComparison.OrdinalIgnoreCase))
        {
            return "bi-arrow-down-up";
        }

        return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "bi-sort-down"
            : "bi-sort-up";
    }

    public string BuildTooltipMessage(IEnumerable<string> messages)
    {
        return string.Join(" ", messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => $"• {NormalizeText(message)}"));
    }

    private async Task LoadSuppliersAsync()
    {
        var result = await suppliersApiAdapter.GetSuppliersAsync(
            SearchTerm,
            SortBy,
            SortDirection,
            HttpContext.RequestAborted);

        if (!result.Success || result.Data == null)
        {
            logger.LogWarning(
                "No se pudieron cargar proveedores desde el API: {Errors}",
                string.Join(" | ", result.Errors));

            ActiveSuppliers = [];
            NextSupplierCodePreview = "PRV001";
            FieldHints = [];
            CreateFieldHints = [];
            EditFieldHints = [];
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo cargar el listado de proveedores.");
            return;
        }

        ActiveSuppliers = result.Data.Suppliers.Select(MapToRow).ToList();
        NextSupplierCodePreview = string.IsNullOrWhiteSpace(result.Data.NextSupplierCode)
            ? "PRV001"
            : result.Data.NextSupplierCode;
        FieldHints = ToMutableHintsDictionary(result.Data.FieldHints);
        CreateFieldHints = BuildPrefixedHintsDictionary(nameof(NewSupplier), FieldHints);
        EditFieldHints = BuildPrefixedHintsDictionary(nameof(EditSupplier), FieldHints);
    }

    private RedirectToPageResult RedirectToCurrentState()
    {
        return RedirectToPage(new
        {
            SearchTerm,
            SortBy,
            SortDirection
        });
    }

    private void NormalizeState()
    {
        SearchTerm = NormalizeText(SearchTerm);
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = NormalizeSortDirection(SortDirection);
    }

    private void EnsureNewSupplierCode()
    {
        if (string.IsNullOrWhiteSpace(NewSupplier.Codigo))
        {
            NewSupplier.Codigo = NextSupplierCodePreview;
        }
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

    private static SaveSupplierRequestDto ToSaveRequest(SupplierFormDto supplier)
    {
        return new SaveSupplierRequestDto(
            NormalizeText(supplier.Codigo).ToUpperInvariant(),
            NormalizeCollapsed(supplier.Nombre),
            NormalizeCollapsed(supplier.Direccion),
            NormalizeCollapsed(supplier.Contacto),
            NormalizeCollapsed(supplier.Rubro),
            NormalizeText(supplier.Telefono));
    }

    private static string NormalizeSortBy(string? value)
    {
        var normalizedValue = NormalizeText(value).ToLowerInvariant();
        return normalizedValue switch
        {
            "code" => "code",
            "contact" => "contact",
            "phone" => "phone",
            "rubro" => "rubro",
            _ => DefaultSortBy
        };
    }

    private static string NormalizeSortDirection(string? value)
    {
        return string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : DefaultSortDirection;
    }

    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string NormalizeCollapsed(string? value)
    {
        return string.Join(' ', NormalizeText(value).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static Dictionary<string, List<string>> ToMutableHintsDictionary(
        IReadOnlyDictionary<string, IReadOnlyList<string>> hints)
    {
        var copy = new Dictionary<string, List<string>>(hints.Count);
        foreach (var hint in hints)
        {
            copy[hint.Key] = [.. hint.Value];
        }

        return copy;
    }

    private static Dictionary<string, List<string>> BuildPrefixedHintsDictionary(
        string prefix,
        IReadOnlyDictionary<string, List<string>> hints)
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
