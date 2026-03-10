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
                if (TotalPages == 0) TotalPages = 1;

                var paged = await _employeeRepository.GetEmployeesByPages(CurrentPage, PageSize);
                Employees = paged?.ToList() ?? new List<Employee>();
                _logger.LogInformation("Empleados cargados: {Count} en página {Page}", Employees.Count, CurrentPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                ModelState.AddModelError(string.Empty, "Error al cargar los empleados.");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            _logger.LogWarning("=== INICIO CREAR EMPLEADO ===");
    _logger.LogWarning("Datos recibidos: Ci={Ci}, Nombres={Nombres}, PrimerApellido={PrimerApellido}, Rol={Rol}, Contacto={NumeroContacto}, Complemento={Complemento}, SegundoApellido={SegundoApellido}", 
        NewEmployee.Ci, NewEmployee.Nombres, NewEmployee.PrimerApellido, NewEmployee.Rol, NewEmployee.NumeroContacto, NewEmployee.Complemento, NewEmployee.SegundoApellido);
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("EditEmployee.")).ToList())
                ModelState.Remove(key);

            

            try
            {
                var id = await _registerEmployeeUseCase.ExecuteAsync(NewEmployee);
                _logger.LogInformation("Empleado creado con ID: {Id}", id);
                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado. Datos: {@NewEmployee}", NewEmployee);
                ModelState.AddModelError(string.Empty, "Error al guardar el empleado: " + ex.Message);
                await LoadEmployeesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("NewEmployee.")).ToList())
                ModelState.Remove(key);

            if (!TryValidateModel(EditEmployee, nameof(EditEmployee)))
            {
                _logger.LogWarning("Validación fallida al editar empleado. Errores: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
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
                    NumeroContacto = EditEmployee.NumeroContacto,
                    Estado = EditEmployee.Estado ?? existing.Estado
                };

                await _updateEmployeeUseCase.ExecuteAsync(updated);
                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado. Datos: {@EditEmployee}", EditEmployee);
                ModelState.AddModelError(string.Empty, "Error al actualizar el empleado: " + ex.Message);
                await LoadEmployeesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            try
            {
                await _employeeRepository.DeleteEmployeeAsync(id);
                TempData["SuccessMessage"] = "Empleado eliminado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado con ID: {Id}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el empleado.";
            }
            return RedirectToPage();
        }
    }
}