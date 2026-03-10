using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.usecases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _employeeManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize, cancellationToken);
                TotalPages = result.TotalPages;

                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    result = await _employeeManagementUseCase.GetPageAsync(CurrentPage, _defaultPageSize, cancellationToken);
                }

                Employees = [.. result.Employees];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                ModelState.AddModelError(string.Empty, "Error al cargar los empleados.");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(int pageNumber = 1)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
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
                await _registerEmployeeUseCase.ExecuteAsync(NewEmployee, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
                return RedirectToPage(new { pageNumber = CurrentPage });
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validacion de negocio al crear empleado");
                ModelState.AddModelError(string.Empty, validationException.Message);
                ShowCreateEmployeeModal = true;
                await LoadEmployeesAsync();
                return Page();
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

        public async Task<IActionResult> OnPostEditAsync(int pageNumber = 1)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;
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
                await _updateEmployeeUseCase.ExecuteAsync(EditEmployee, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
                return RedirectToPage(new { pageNumber = CurrentPage });
            }
            catch (InvalidOperationException invalidOperationException)
            {
                _logger.LogWarning(invalidOperationException, "Empleado no encontrado al actualizar");
                ModelState.AddModelError(string.Empty, invalidOperationException.Message);
                await LoadEmployeesAsync();
                ShowEditEmployeeModal = true;
                return Page();
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

        public async Task<IActionResult> OnPostDeleteAsync(long id, int pageNumber = 1)
        {
            CurrentPage = pageNumber > 0 ? pageNumber : 1;

            try
            {
                var deleted = await _employeeManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Empleado eliminado."
                    : "Empleado no encontrado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado");
                TempData["ErrorMessage"] = "No se pudo eliminar el empleado.";
            }
            return RedirectToPage(new { pageNumber = CurrentPage });
        }
    }
}
