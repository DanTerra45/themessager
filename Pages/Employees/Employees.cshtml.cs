using Mercadito.src.employees.application.models;
using Mercadito.src.employees.application.ports.input;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.shared.domain.validation;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Employees
{
    public partial class EmployeesModel(
        ILogger<EmployeesModel> logger,
        IListingPageStateService listingPageStateService,
        IModalPostbackStateService modalPostbackStateService,
        IEmployeeManagementUseCase employeeManagementUseCase,
        IConfiguration configuration) : AppPageModel
    {
        private const string CurrentPageSessionKey = "Employees.CurrentPage";
        private const string CurrentAnchorEmployeeIdSessionKey = "Employees.CurrentAnchorEmployeeId";
        private const string PendingNavigationModeSessionKey = "Employees.PendingNavigationMode";
        private const string PendingNavigationCursorEmployeeIdSessionKey = "Employees.PendingNavigationCursorEmployeeId";
        private const string EditEmployeeSessionKey = "Employees.EditEmployeeId";
        private const string PendingCreateModalSessionKey = "Employees.PendingCreateModal";
        private const string PendingCreateDraftSessionKey = "Employees.PendingCreateDraft";
        private const string PendingCreateErrorsSessionKey = "Employees.PendingCreateErrors";
        private const string PendingEditModalSessionKey = "Employees.PendingEditModal";
        private const string PendingEditDraftSessionKey = "Employees.PendingEditDraft";
        private const string PendingEditErrorsSessionKey = "Employees.PendingEditErrors";
        private const string SortBySessionKey = "Employees.SortBy";
        private const string SortDirectionSessionKey = "Employees.SortDirection";
        private const string SearchTermSessionKey = "Employees.SearchTerm";
        private const string DefaultSortBy = "apellidos";
        private const string DefaultSortDirection = "asc";
        private static readonly KeysetListingSessionKeys ListingSessionKeys = new(
            CurrentPageSessionKey,
            CurrentAnchorEmployeeIdSessionKey,
            PendingNavigationModeSessionKey,
            PendingNavigationCursorEmployeeIdSessionKey,
            SortBySessionKey,
            SortDirectionSessionKey,
            SearchTermSessionKey);
        private static readonly ListingPageStateOptions ListingStateOptions = new(
            ListingSessionKeys,
            DefaultSortBy,
            DefaultSortDirection,
            NormalizeSortBy,
            NormalizeSortDirection,
            ValidationText.NormalizeTrimmed);

        private readonly ILogger<EmployeesModel> _logger = logger;
        private readonly IListingPageStateService _listingPageStateService = listingPageStateService;
        private readonly IModalPostbackStateService _modalPostbackStateService = modalPostbackStateService;
        private readonly IEmployeeManagementUseCase _employeeManagementUseCase = employeeManagementUseCase;
        private readonly int _defaultPageSize = PaginationSettings.ResolveDefaultPageSize(configuration);

        public IReadOnlyList<EmployeeModel> Employees { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorEmployeeId { get; set; }
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public string SearchTerm { get; set; } = string.Empty;

        public CreateEmployeeDto NewEmployee { get; set; } = new CreateEmployeeDto();
        public UpdateEmployeeDto EditEmployee { get; set; } = new UpdateEmployeeDto();
        public bool ShowCreateEmployeeModal { get; set; }
        public bool ShowEditEmployeeModal { get; set; }

        public async Task OnGetAsync()
        {
            LoadStateFromSession();
            NormalizeCurrentState();

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadEmployeesByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorId);
            }
            else
            {
                await LoadEmployeesFromAnchorAsync();
            }

            SaveStateInSession();
            RestorePendingPostbackState();
            RestoreModelStateErrors(PendingCreateErrorsSessionKey, _logger);
            RestoreModelStateErrors(PendingEditErrorsSessionKey, _logger);

            if (ShowCreateEmployeeModal || ShowEditEmployeeModal)
            {
                return;
            }

            var editEmployeeId = PopPendingEditEmployeeId();
            if (editEmployeeId <= 0)
            {
                return;
            }

            var employeeForEdit = await _employeeManagementUseCase.GetForEditAsync(editEmployeeId, HttpContext.RequestAborted);
            if (employeeForEdit != null)
            {
                EditEmployee = employeeForEdit;
                ShowEditEmployeeModal = true;
            }
        }

        public IActionResult OnPostFilter(string sortBy = "", string sortDirection = "", string searchTerm = "", bool clear = false)
        {
            LoadStateFromSession();
        var effectiveSearchTerm = searchTerm;
        if (clear)
        {
            effectiveSearchTerm = string.Empty;
        }

        SetSearchAndSortState(effectiveSearchTerm, sortBy, sortDirection);
            CurrentPage = 1;
            CurrentAnchorEmployeeId = 0;

            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigate(
            string navigationMode = "",
            long cursorEmployeeId = 0,
            string sortBy = "",
            string sortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
            SetPendingNavigation(navigationMode, cursorEmployeeId);

            ClearPendingEditEmployeeId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, currentSortBy, currentSortDirection);
            ToggleSort(sortBy);
            CurrentPage = 1;
            CurrentAnchorEmployeeId = 0;

            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEdit(long id, string sortBy = "", string sortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
            ClearPendingNavigation();
            SaveStateInSession();

            if (id > 0)
            {
                SetPendingEditEmployeeId(id);
            }

            return RedirectToPage();
        }

    }
}
