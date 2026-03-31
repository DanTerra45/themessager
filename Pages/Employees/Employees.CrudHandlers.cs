using Mercadito.src.employees.domain.dto;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.Pages.Employees
{
    public partial class EmployeesModel
    {
        public async Task<IActionResult> OnPostCreateAsync(
            [Bind(Prefix = "NewEmployee")] CreateEmployeeDto newEmployee,
            string sortBy = "",
            string sortDirection = "")
        {
            NewEmployee = newEmployee;
            SetSortState(sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewEmployee);
                StorePendingValidationErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _employeeManagementUseCase.CreateAsync(NewEmployee, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al crear empleado");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingCreateModal(NewEmployee);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear empleado");
                TempData["ErrorMessage"] = "Error al guardar el empleado.";
                StorePendingCreateModal(NewEmployee);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(
            [Bind(Prefix = "EditEmployee")] UpdateEmployeeDto editEmployee,
            string sortBy = "",
            string sortDirection = "")
        {
            EditEmployee = editEmployee;
            SetSortState(sortBy, sortDirection);
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditEmployee);
                StorePendingValidationErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _employeeManagementUseCase.UpdateAsync(EditEmployee, HttpContext.RequestAborted);
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
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar empleado");
                TempData["ErrorMessage"] = "Error al actualizar el empleado.";
                StorePendingEditModal(EditEmployee);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, string sortBy = "", string sortDirection = "")
        {
            SetSortState(sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();

            try
            {
                var deleted = await _employeeManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Empleado desactivado."
                    : "El empleado no existe o ya estaba desactivado.";
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al eliminar empleado");
                TempData["ErrorMessage"] = "No se pudo eliminar el empleado.";
            }

            return RedirectToCurrentState();
        }
    }
}
