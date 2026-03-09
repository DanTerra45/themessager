using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Mercadito.Pages.Employees
{
    public class EmployeesModel : PageModel
    {
        private readonly ILogger<EmployeesModel> _logger;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly RegisterEmployeeUseCase _registerEmployeeUseCase;
        private readonly UpdateEmployeeUseCase _updateEmployeeUseCase;

        public List<Employee> Employees { get; set; } = new List<Employee>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        private const int PageSize = 10;

        [BindProperty]
        public CreateEmployeeDto NewEmployee { get; set; } = new CreateEmployeeDto();

        [BindProperty]
        public UpdateEmployeeDto EditEmployee { get; set; } = new UpdateEmployeeDto();

        public EmployeesModel(
            ILogger<EmployeesModel> logger,
            IEmployeeRepository employeeRepository,
            RegisterEmployeeUseCase registerEmployeeUseCase,
            UpdateEmployeeUseCase updateEmployeeUseCase)
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
            _registerEmployeeUseCase = registerEmployeeUseCase;
            _updateEmployeeUseCase = updateEmployeeUseCase;
        }

        public async Task OnGetAsync(int? page)
        {
            CurrentPage = page ?? 1;
            await LoadEmployeesAsync();
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var allEmployees = (await _employeeRepository.GetAllEmployeesAsync()).ToList();

                TotalPages = (int)Math.Ceiling(allEmployees.Count / (double)PageSize);
                if (TotalPages == 0)
                    TotalPages = 1;

                var paged = await _employeeRepository.GetEmployeesByPages(CurrentPage, PageSize);
                Employees = paged?.ToList() ?? new List<Employee>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                ModelState.AddModelError(string.Empty, "Error al cargar los empleados.");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            _logger.LogWarning("=== ONSOLICITUD POST CREATE RECIBIDA ===");

            // Limpiar ModelState para evitar conflictos con EditEmployee
            ModelState.Clear();

            // Validar SOLO el modelo de creación
            TryValidateModel(NewEmployee, nameof(NewEmployee));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                _logger.LogWarning("Errores de validación CREATE: {Errors}", string.Join(", ", errors));

                await LoadEmployeesAsync();
                return Page();
            }

            try
            {
                var id = await _registerEmployeeUseCase.ExecuteAsync(NewEmployee);

                _logger.LogWarning("Empleado creado con ID: {Id}", id);

                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado");

                ModelState.AddModelError(string.Empty, "Error al guardar el empleado.");

                await LoadEmployeesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            // Eliminar validaciones de Create
            ModelState.Remove("NewEmployee");
            ModelState.Remove("NewEmployee.Ci");
            ModelState.Remove("NewEmployee.Nombres");
            ModelState.Remove("NewEmployee.PrimerApellido");
            ModelState.Remove("NewEmployee.SegundoApellido");
            ModelState.Remove("NewEmployee.Rol");
            ModelState.Remove("NewEmployee.NumeroContacto");

            ModelState.Clear();

            // Validar SOLO EditEmployee
            TryValidateModel(EditEmployee, nameof(EditEmployee));

            _logger.LogWarning("=== ONSOLICITUD POST EDIT RECIBIDA ===");
            _logger.LogWarning("Id: {Id}", EditEmployee.Id);
            _logger.LogWarning("Nombres: {Nombres}", EditEmployee.Nombres);
            _logger.LogWarning("Rol: {Rol}", EditEmployee.Rol);
            _logger.LogWarning("NumeroContacto: {NumeroContacto}", EditEmployee.NumeroContacto);
            _logger.LogWarning("ModelState.IsValid: {IsValid}", ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                _logger.LogWarning("Errores de validación EDIT: {Errors}", string.Join(", ", errors));

                await LoadEmployeesAsync();
                TempData["ShowEditModal"] = "true";

                return Page();
            }

            try
            {
                var existing = await _employeeRepository.GetEmployeeByIdAsync(EditEmployee.Id);

                if (existing == null)
                {
                    ModelState.AddModelError(string.Empty, "Empleado no encontrado.");
                    await LoadEmployeesAsync();
                    return Page();
                }

                var updated = new Employee
                {
                    Id = EditEmployee.Id,
                    Ci = EditEmployee.Ci,
                    Complemento = EditEmployee.Complemento,
                    Nombres = EditEmployee.Nombres,
                    PrimerApellido = EditEmployee.PrimerApellido,
                    SegundoApellido = EditEmployee.SegundoApellido,
                    Rol = EditEmployee.Rol,
                    NumeroContacto = EditEmployee.NumeroContacto
                };

                await _updateEmployeeUseCase.ExecuteAsync(updated);

                _logger.LogWarning("Empleado actualizado con ID: {Id}", EditEmployee.Id);

                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado");

                ModelState.AddModelError(string.Empty, "Error al actualizar el empleado.");

                await LoadEmployeesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                await _employeeRepository.DeleteEmployeeAsync(id);

                TempData["SuccessMessage"] = "Empleado eliminado.";
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