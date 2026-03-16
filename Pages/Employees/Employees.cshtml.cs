using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.usecases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace Mercadito.Pages.Employees
{
    public class EmployeesModel : PageModel
    {
        private const string CurrentPageSessionKey = "Employees.CurrentPage";
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

        private readonly ILogger<EmployeesModel> _logger;
        private readonly IEmployeeManagementUseCase _employeeManagementUseCase;
        private readonly IRegisterEmployeeUseCase _registerEmployeeUseCase;
        private readonly IUpdateEmployeeUseCase _updateEmployeeUseCase;
        private readonly int _defaultPageSize;

        public List<Employee> Employees { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;

        public CreateEmployeeDto NewEmployee { get; set; } = new CreateEmployeeDto();

        public UpdateEmployeeDto EditEmployee { get; set; } = new UpdateEmployeeDto();

        public bool ShowCreateEmployeeModal { get; set; }

        public bool ShowEditEmployeeModal { get; set; }

        public EmployeesModel(
            ILogger<EmployeesModel> logger,
            IEmployeeManagementUseCase employeeManagementUseCase,
            IRegisterEmployeeUseCase registerEmployeeUseCase,
            IUpdateEmployeeUseCase updateEmployeeUseCase,
            IConfiguration configuration)
        {
            _logger = logger;
            _employeeManagementUseCase = employeeManagementUseCase;
            _registerEmployeeUseCase = registerEmployeeUseCase;
            _updateEmployeeUseCase = updateEmployeeUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public async Task OnGetAsync()
        {
            LoadCurrentPageFromSession();
            LoadSortStateFromSession();
            await LoadEmployeesAsync();
            SaveCurrentPageInSession();
            SaveSortStateInSession();
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
                EditEmployee = new UpdateEmployeeDto
                {
                    Id = employeeForEdit.Id,
                    Ci = employeeForEdit.Ci,
                    Complemento = employeeForEdit.Complemento,
                    Nombres = employeeForEdit.Nombres,
                    PrimerApellido = employeeForEdit.PrimerApellido,
                    SegundoApellido = employeeForEdit.SegundoApellido,
                    NumeroContacto = employeeForEdit.NumeroContacto,
                    Rol = employeeForEdit.Rol
                };

                ShowEditEmployeeModal = true;
            }
        }

        public IActionResult OnPostNavigate(int pageNumber = 1, string sortBy = "", string sortDirection = "")
        {
            SetCurrentPage(pageNumber);
            SetSortState(sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            SaveCurrentPageInSession();
            SaveSortStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            SetCurrentPage(1);
            SetSortState(currentSortBy, currentSortDirection);
            ToggleSort(sortBy);

            ClearPendingEditEmployeeId();
            SaveCurrentPageInSession();
            SaveSortStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostStartEdit(long id, int pageNumber = 1, string sortBy = "", string sortDirection = "")
        {
            SetCurrentPage(pageNumber);
            SetSortState(sortBy, sortDirection);
            SaveCurrentPageInSession();
            SaveSortStateInSession();

            if (id > 0)
            {
                SetPendingEditEmployeeId(id);
            }

            return RedirectToPage();
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _employeeManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize, SortBy, SortDirection, cancellationToken);
                var maxPage = Math.Max(result.TotalPages, 1);

                if (CurrentPage > maxPage)
                {
                    CurrentPage = maxPage;
                    result = await _employeeManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize, SortBy, SortDirection, cancellationToken);
                }

                TotalPages = Math.Max(result.TotalPages, 1);
                Employees = [.. result.Employees];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                ModelState.AddModelError(string.Empty, "Error al cargar los empleados.");
                Employees = [];
                TotalPages = 1;
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(
            [Bind(Prefix = "NewEmployee")] CreateEmployeeDto newEmployee,
            int pageNumber = 1,
            string sortBy = "",
            string sortDirection = "")
        {
            NewEmployee = newEmployee;
            SetCurrentPage(pageNumber);
            SetSortState(sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            SaveCurrentPageInSession();
            SaveSortStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewEmployee);
                StorePendingValidationErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _registerEmployeeUseCase.ExecuteAsync(NewEmployee, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validacion de negocio al crear empleado");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingCreateModal(NewEmployee);
                return RedirectToCurrentState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado");
                TempData["ErrorMessage"] = "Error al guardar el empleado.";
                StorePendingCreateModal(NewEmployee);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(
            [Bind(Prefix = "EditEmployee")] UpdateEmployeeDto editEmployee,
            int pageNumber = 1,
            string sortBy = "",
            string sortDirection = "")
        {
            EditEmployee = editEmployee;
            SetCurrentPage(pageNumber);
            SetSortState(sortBy, sortDirection);
            SaveCurrentPageInSession();
            SaveSortStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditEmployee);
                StorePendingValidationErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _updateEmployeeUseCase.ExecuteAsync(EditEmployee, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar empleado");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingEditModal(EditEmployee);
                return RedirectToCurrentState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado");
                TempData["ErrorMessage"] = "Error al actualizar el empleado.";
                StorePendingEditModal(EditEmployee);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, int pageNumber = 1, string sortBy = "", string sortDirection = "")
        {
            SetCurrentPage(pageNumber);
            SetSortState(sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            SaveCurrentPageInSession();
            SaveSortStateInSession();

            try
            {
                var deleted = await _employeeManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Empleado desactivado."
                    : "El empleado no existe o ya estaba desactivado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado");
                TempData["ErrorMessage"] = "No se pudo eliminar el empleado.";
            }

            return RedirectToCurrentState();
        }

        private RedirectToPageResult RedirectToCurrentState()
        {
            ClearPendingEditEmployeeId();
            SaveCurrentPageInSession();
            SaveSortStateInSession();
            return RedirectToPage();
        }

        private void StorePendingCreateModal(CreateEmployeeDto draft)
        {
            HttpContext.Session.SetString(PendingCreateModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingCreateDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void StorePendingEditModal(UpdateEmployeeDto draft)
        {
            HttpContext.Session.SetString(PendingEditModalSessionKey, bool.TrueString);
            HttpContext.Session.SetString(PendingEditDraftSessionKey, JsonSerializer.Serialize(draft));
        }

        private void RestorePendingPostbackState()
        {
            if (PopFlag(PendingCreateModalSessionKey))
            {
                ShowCreateEmployeeModal = true;
                var pendingCreateDraft = PopDraft<CreateEmployeeDto>(PendingCreateDraftSessionKey);
                if (pendingCreateDraft != null)
                {
                    NewEmployee = pendingCreateDraft;
                }
            }
            else
            {
                HttpContext.Session.Remove(PendingCreateDraftSessionKey);
            }

            if (PopFlag(PendingEditModalSessionKey))
            {
                ShowEditEmployeeModal = true;
                var pendingEditDraft = PopDraft<UpdateEmployeeDto>(PendingEditDraftSessionKey);
                if (pendingEditDraft != null)
                {
                    EditEmployee = pendingEditDraft;
                }
            }
            else
            {
                HttpContext.Session.Remove(PendingEditDraftSessionKey);
            }
        }

        private bool PopFlag(string sessionKey)
        {
            var rawValue = HttpContext.Session.GetString(sessionKey);
            HttpContext.Session.Remove(sessionKey);

            return bool.TryParse(rawValue, out var parsedValue) && parsedValue;
        }

        private T? PopDraft<T>(string sessionKey) where T : class
        {
            var rawValue = HttpContext.Session.GetString(sessionKey);
            HttpContext.Session.Remove(sessionKey);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(rawValue);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "No se pudo restaurar el borrador temporal de modal para key {SessionKey}", sessionKey);
                return null;
            }
        }

        private void StorePendingValidationErrors(string sessionKey)
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Valor invalido." : error.ErrorMessage)
                        .ToArray());

            if (errors.Count == 0)
            {
                HttpContext.Session.Remove(sessionKey);
                return;
            }

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(errors));
        }

        private void RestorePendingValidationErrors(string sessionKey)
        {
            var rawValue = HttpContext.Session.GetString(sessionKey);
            HttpContext.Session.Remove(sessionKey);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            try
            {
                var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(rawValue);
                if (errors == null)
                {
                    return;
                }

                foreach (var (key, messages) in errors)
                {
                    if (messages == null)
                    {
                        continue;
                    }

                    foreach (var message in messages)
                    {
                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            ModelState.AddModelError(key, message);
                        }
                    }
                }
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "No se pudo restaurar errores de validacion para key {SessionKey}", sessionKey);
            }
        }

        public string GetSortIcon(string columnName)
        {
            var normalizedColumn = NormalizeSortBy(columnName);
            if (!string.Equals(SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return "bi-arrow-down-up";
            }

            return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "bi-sort-down"
                : "bi-sort-up";
        }

        private void LoadCurrentPageFromSession()
        {
            var currentPageInSession = HttpContext.Session.GetInt32(CurrentPageSessionKey);
            if (!currentPageInSession.HasValue || currentPageInSession.Value <= 0)
            {
                CurrentPage = 1;
                return;
            }

            CurrentPage = currentPageInSession.Value;
        }

        private void SetCurrentPage(int pageNumber)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
        }

        private void SetSortState(string sortBy, string sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy) && string.IsNullOrWhiteSpace(sortDirection))
            {
                LoadSortStateFromSession();
                return;
            }

            SortBy = NormalizeSortBy(sortBy);
            SortDirection = NormalizeSortDirection(sortDirection);
        }

        private void SaveCurrentPageInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
        }

        private void LoadSortStateFromSession()
        {
            var sortByInSession = HttpContext.Session.GetString(SortBySessionKey);
            var sortDirectionInSession = HttpContext.Session.GetString(SortDirectionSessionKey);

            SortBy = NormalizeSortBy(sortByInSession is string persistedSortBy ? persistedSortBy : string.Empty);
            SortDirection = NormalizeSortDirection(sortDirectionInSession is string persistedSortDirection ? persistedSortDirection : string.Empty);
        }

        private void SaveSortStateInSession()
        {
            HttpContext.Session.SetString(SortBySessionKey, NormalizeSortBy(SortBy));
            HttpContext.Session.SetString(SortDirectionSessionKey, NormalizeSortDirection(SortDirection));
        }

        private void ToggleSort(string sortBy)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            if (string.Equals(SortBy, normalizedSortBy, StringComparison.OrdinalIgnoreCase))
            {
                SortDirection = string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                return;
            }

            SortBy = normalizedSortBy;
            SortDirection = DefaultSortDirection;
        }

        private static string NormalizeSortBy(string sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return DefaultSortBy;
            }

            var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "id" => "id",
                "ci" => "ci",
                "nombres" => "nombres",
                "rol" => "rol",
                _ => "apellidos"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";
        }

        private void SetPendingEditEmployeeId(long employeeId)
        {
            HttpContext.Session.SetString(EditEmployeeSessionKey, employeeId.ToString(CultureInfo.InvariantCulture));
        }

        private long PopPendingEditEmployeeId()
        {
            var rawEditEmployeeId = HttpContext.Session.GetString(EditEmployeeSessionKey);
            HttpContext.Session.Remove(EditEmployeeSessionKey);

            return long.TryParse(rawEditEmployeeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var editEmployeeId)
                ? editEmployeeId
                : 0;
        }

        private void ClearPendingEditEmployeeId()
        {
            HttpContext.Session.Remove(EditEmployeeSessionKey);
        }
    }
}
