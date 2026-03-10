using Mercadito.src.employees.data.dto;
using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.usecases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Employees
{
    public class EmployeesModel : PageModel
    {
        private readonly ILogger<EmployeesModel> _logger;
        private readonly IEmployeeManagementUseCase _employeeManagementUseCase;
        private readonly IRegisterEmployeeUseCase _registerEmployeeUseCase;
        private readonly IUpdateEmployeeUseCase _updateEmployeeUseCase;
        private readonly int _defaultPageSize;

        public List<Employee> Employees { get; set; } = [];
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        [BindProperty]
        [ValidateNever]
        public CreateEmployeeDto NewEmployee { get; set; } = new CreateEmployeeDto();

        [BindProperty]
        [ValidateNever]
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

        public async Task OnGetAsync(int pageNumber = 1)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
            await LoadEmployeesAsync();
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var result = await _employeeManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize);
                TotalPages = result.TotalPages;

                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    result = await _employeeManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize);
                }

                Employees = [.. result.Employees];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                ModelState.AddModelError(string.Empty, "Error al cargar los empleados.");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();
            ModelState.ClearValidationState(nameof(NewEmployee));
            var isValid = TryValidateModel(NewEmployee, nameof(NewEmployee));

            if (!isValid)
            {
                ShowCreateEmployeeModal = true;
                await LoadEmployeesAsync();
                return Page();
            }

            try
            {
                await _registerEmployeeUseCase.ExecuteAsync(NewEmployee);
                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado");
                ModelState.AddModelError(string.Empty, "Error al guardar el empleado.");
                ShowCreateEmployeeModal = true;
                await LoadEmployeesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            ModelState.Clear();
            ModelState.ClearValidationState(nameof(EditEmployee));
            var isValid = TryValidateModel(EditEmployee, nameof(EditEmployee));

            if (!isValid)
            {
                await LoadEmployeesAsync();
                ShowEditEmployeeModal = true;
                return Page();
            }

            try
            {
                var existing = await _employeeManagementUseCase.GetForEditAsync(EditEmployee.Id);
                if (existing == null)
                {
                    ModelState.AddModelError(string.Empty, "Empleado no encontrado.");
                    await LoadEmployeesAsync();
                    return Page();
                }

                await _updateEmployeeUseCase.ExecuteAsync(EditEmployee);
                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado");
                ModelState.AddModelError(string.Empty, "Error al actualizar el empleado.");
                await LoadEmployeesAsync();
                ShowEditEmployeeModal = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                var deleted = await _employeeManagementUseCase.DeleteAsync(id);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Empleado eliminado."
                    : "Empleado no encontrado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado");
                TempData["ErrorMessage"] = "No se pudo eliminar el empleado.";
            }
            return RedirectToPage();
        }
    }
}