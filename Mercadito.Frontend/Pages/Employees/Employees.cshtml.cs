using Mercadito.Frontend.Adapters.Employees;
using Mercadito.Frontend.Dtos.Employees;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Employees;

public sealed class EmployeesModel(
    IEmployeesApiAdapter employeesApiAdapter,
    IConfiguration configuration,
    ILogger<EmployeesModel> logger) : FrontendPageModel
{
    private const string DefaultSortBy = "apellidos";
    private const string DefaultSortDirection = "asc";

    private readonly int _defaultPageSize = ResolveDefaultPageSize(configuration);

    public IReadOnlyList<EmployeeDto> Employees { get; private set; } = [];
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }
    public bool ShowCreateEmployeeModal { get; private set; }
    public bool ShowEditEmployeeModal { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public long CurrentAnchorEmployeeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = DefaultSortDirection;

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty]
    public EmployeeFormDto NewEmployee { get; set; } = new();

    [BindProperty]
    public EmployeeFormDto EditEmployee { get; set; } = new();

    public async Task OnGetAsync()
    {
        NormalizeState();
        await LoadEmployeesAsync(useCursor: false, cursorEmployeeId: 0, isNextPage: true);
    }

    public IActionResult OnPostFilter(
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "",
        bool clear = false)
    {
        return RedirectToPage(new
        {
            SortBy = NormalizeSortBy(sortBy),
            SortDirection = NormalizeSortDirection(sortDirection),
            SearchTerm = clear ? string.Empty : NormalizeText(searchTerm),
            CurrentPage = 1,
            CurrentAnchorEmployeeId = 0
        });
    }

    public async Task<IActionResult> OnPostNavigateAsync(
        string navigationMode = "",
        long cursorEmployeeId = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "",
        int currentPage = 1)
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);
        CurrentPage = Math.Max(1, currentPage);

        var isNextPage = string.Equals(navigationMode, "next", StringComparison.OrdinalIgnoreCase);
        CurrentPage = isNextPage ? CurrentPage + 1 : Math.Max(1, CurrentPage - 1);

        await LoadEmployeesAsync(useCursor: cursorEmployeeId > 0, cursorEmployeeId, isNextPage);
        return Page();
    }

    public IActionResult OnPostSort(
        string sortBy = "",
        string currentSortBy = "",
        string currentSortDirection = "",
        string searchTerm = "")
    {
        SearchTerm = NormalizeText(searchTerm);
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
            SortBy = normalizedRequestedSortBy,
            SortDirection = nextDirection,
            SearchTerm,
            CurrentPage = 1,
            CurrentAnchorEmployeeId = 0
        });
    }

    public async Task<IActionResult> OnPostStartEditAsync(
        long id,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);

        var result = await employeesApiAdapter.GetEmployeeAsync(id, HttpContext.RequestAborted);
        if (result.Success && result.Data != null)
        {
            EditEmployee = ToForm(result.Data);
            ShowEditEmployeeModal = true;
        }
        else
        {
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo cargar el empleado.");
        }

        await LoadEmployeesAsync(useCursor: false, cursorEmployeeId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);
        RemoveModelStateForPrefix(nameof(EditEmployee));

        if (!IsModelStateValidForPrefix(nameof(NewEmployee)))
        {
            LogInvalidModelState(logger, "Employees.Create");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
            ShowCreateEmployeeModal = true;
            await LoadEmployeesAsync(useCursor: false, cursorEmployeeId: 0, isNextPage: true);
            return Page();
        }

        var result = await employeesApiAdapter.CreateEmployeeAsync(
            ToSaveRequest(NewEmployee),
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
            return RedirectToCurrentState(resetToFirstPage: true);
        }

        ApplyApiErrors(result, nameof(NewEmployee));
        TempData["ErrorMessage"] = FirstErrorOrDefault(result, "Corrige los errores del formulario.");
        ShowCreateEmployeeModal = true;
        await LoadEmployeesAsync(useCursor: false, cursorEmployeeId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);
        RemoveModelStateForPrefix(nameof(NewEmployee));

        if (!IsModelStateValidForPrefix(nameof(EditEmployee)))
        {
            LogInvalidModelState(logger, "Employees.Edit");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
            ShowEditEmployeeModal = true;
            await LoadEmployeesAsync(useCursor: false, cursorEmployeeId: 0, isNextPage: true);
            return Page();
        }

        var result = await employeesApiAdapter.UpdateEmployeeAsync(
            EditEmployee.Id,
            ToSaveRequest(EditEmployee),
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
            return RedirectToCurrentState(resetToFirstPage: false);
        }

        ApplyApiErrors(result, nameof(EditEmployee));
        TempData["ErrorMessage"] = FirstErrorOrDefault(result, "Corrige los errores del formulario.");
        ShowEditEmployeeModal = true;
        await LoadEmployeesAsync(useCursor: false, cursorEmployeeId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        long id,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);

        var result = await employeesApiAdapter.DeleteEmployeeAsync(
            id,
            BuildActorContext(),
            HttpContext.RequestAborted);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Empleado desactivado."
            : FirstErrorOrDefault(result, "No se pudo eliminar el empleado.");

        return RedirectToCurrentState(resetToFirstPage: false);
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

    private async Task LoadEmployeesAsync(bool useCursor, long cursorEmployeeId, bool isNextPage)
    {
        var result = await employeesApiAdapter.GetEmployeesAsync(
            _defaultPageSize,
            SortBy,
            SortDirection,
            useCursor ? 0 : CurrentAnchorEmployeeId,
            useCursor ? cursorEmployeeId : 0,
            isNextPage,
            SearchTerm,
            HttpContext.RequestAborted);

        if (!result.Success || result.Data == null)
        {
            logger.LogWarning(
                "No se pudieron cargar empleados desde el API: {Errors}",
                string.Join(" | ", result.Errors));

            Employees = [];
            HasPreviousPage = false;
            HasNextPage = false;
            CurrentAnchorEmployeeId = 0;
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudieron cargar los empleados.");
            return;
        }

        Employees = result.Data.Employees;
        HasPreviousPage = CurrentPage > 1 && result.Data.HasPreviousPage;
        HasNextPage = result.Data.HasNextPage;
        CurrentAnchorEmployeeId = Employees.Count > 0 ? Employees[0].Id : 0;
    }

    private RedirectToPageResult RedirectToCurrentState(bool resetToFirstPage)
    {
        return RedirectToPage(new
        {
            SortBy,
            SortDirection,
            SearchTerm,
            CurrentPage = resetToFirstPage ? 1 : CurrentPage,
            CurrentAnchorEmployeeId = resetToFirstPage ? 0 : CurrentAnchorEmployeeId
        });
    }

    private void NormalizeState()
    {
        CurrentPage = Math.Max(1, CurrentPage);
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = NormalizeSortDirection(SortDirection);
        SearchTerm = NormalizeText(SearchTerm);
    }

    private static EmployeeFormDto ToForm(EmployeeDto employee)
    {
        return new EmployeeFormDto
        {
            Id = employee.Id,
            Ci = employee.Ci,
            Complemento = employee.Complemento,
            Nombres = employee.Nombres,
            PrimerApellido = employee.PrimerApellido,
            SegundoApellido = employee.SegundoApellido,
            Cargo = employee.Cargo,
            NumeroContacto = employee.NumeroContacto
        };
    }

    private static SaveEmployeeRequestDto ToSaveRequest(EmployeeFormDto employee)
    {
        return new SaveEmployeeRequestDto(
            employee.Ci,
            NormalizeText(employee.Complemento).ToUpperInvariant(),
            NormalizeCollapsed(employee.Nombres),
            NormalizeCollapsed(employee.PrimerApellido),
            NormalizeOptionalCollapsed(employee.SegundoApellido),
            NormalizeText(employee.Cargo),
            NormalizeText(employee.NumeroContacto));
    }

    private static string NormalizeSortBy(string sortBy)
    {
        var normalized = NormalizeText(sortBy).ToLowerInvariant();
        return normalized switch
        {
            "id" => "id",
            "ci" => "ci",
            "nombres" => "nombres",
            "cargo" => "cargo",
            "rol" => "cargo",
            _ => DefaultSortBy
        };
    }

    private static string NormalizeSortDirection(string sortDirection)
    {
        return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
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

    private static string? NormalizeOptionalCollapsed(string? value)
    {
        var normalized = NormalizeCollapsed(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int ResolveDefaultPageSize(IConfiguration configuration)
    {
        return int.TryParse(configuration["Ui:DefaultPageSize"], out var pageSize)
            ? Math.Clamp(pageSize, 5, 50)
            : 10;
    }
}
