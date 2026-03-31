using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.model;
using Mercadito.src.employees.domain.usecases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace Mercadito.Pages.Employees
{
    public partial class EmployeesModel : PageModel
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
        private const string DefaultSortBy = "apellidos";
        private const string DefaultSortDirection = "asc";
        private const string NavigationModeNext = "next";
        private const string NavigationModePrevious = "prev";

        private readonly ILogger<EmployeesModel> _logger;
        private readonly IEmployeeManagementUseCase _employeeManagementUseCase;
        private readonly int _defaultPageSize;

        public List<EmployeeModel> Employees { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorEmployeeId { get; set; }
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;

        public CreateEmployeeDto NewEmployee { get; set; } = new CreateEmployeeDto();
        public UpdateEmployeeDto EditEmployee { get; set; } = new UpdateEmployeeDto();
        public bool ShowCreateEmployeeModal { get; set; }
        public bool ShowEditEmployeeModal { get; set; }

        public EmployeesModel(
            ILogger<EmployeesModel> logger,
            IEmployeeManagementUseCase employeeManagementUseCase,
            IConfiguration configuration)
        {
            _logger = logger;
            _employeeManagementUseCase = employeeManagementUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public async Task OnGetAsync()
        {
            LoadStateFromSession();
            NormalizeCurrentState();

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadEmployeesByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorEmployeeId);
            }
            else
            {
                await LoadEmployeesFromAnchorAsync();
            }

            SaveStateInSession();
            RestorePendingPostbackState();
            RestorePendingValidationErrors(PendingCreateErrorsSessionKey);
            RestorePendingValidationErrors(PendingEditErrorsSessionKey);

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

        public IActionResult OnPostNavigate(
            string navigationMode = "",
            long cursorEmployeeId = 0,
            string sortBy = "",
            string sortDirection = "")
        {
            LoadStateFromSession();
            SetSortState(sortBy, sortDirection);
            SetPendingNavigation(navigationMode, cursorEmployeeId);

            ClearPendingEditEmployeeId();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            SetSortState(currentSortBy, currentSortDirection);
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
            SetSortState(sortBy, sortDirection);
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

