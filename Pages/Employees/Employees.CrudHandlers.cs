using Mercadito.src.employees.application.models;
using Microsoft.AspNetCore.Mvc;
using Mercadito.src.shared.domain.exceptions;

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
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewEmployee);
                StoreModelStateErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                var actor = BuildAuditActor();
                var result = await _employeeManagementUseCase.CreateAsync(NewEmployee, actor, HttpContext.RequestAborted);
                if (result.IsFailure)
                {
                    if (result.Errors.Count > 0)
                    {
                        ApplyValidationErrors("NewEmployee", result.Errors);
                        StoreModelStateErrors(PendingCreateErrorsSessionKey);
                        TempData["ErrorMessage"] = "Corrige los errores del formulario.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    StorePendingCreateModal(NewEmployee);
                    return RedirectToCurrentState();
                }

                TempData["SuccessMessage"] = "Empleado agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (DataStoreUnavailableException exception)
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
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditEmployee);
                StoreModelStateErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                var actor = BuildAuditActor();
                var result = await _employeeManagementUseCase.UpdateAsync(EditEmployee, actor, HttpContext.RequestAborted);
                if (result.IsFailure)
                {
                    if (result.Errors.Count > 0)
                    {
                        ApplyValidationErrors("EditEmployee", result.Errors);
                        StoreModelStateErrors(PendingEditErrorsSessionKey);
                        TempData["ErrorMessage"] = "Corrige los errores del formulario.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    StorePendingEditModal(EditEmployee);
                    return RedirectToCurrentState();
                }

                TempData["SuccessMessage"] = "Empleado actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Error al actualizar empleado");
                TempData["ErrorMessage"] = "Error al actualizar el empleado.";
                StorePendingEditModal(EditEmployee);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, string sortBy = "", string sortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);

            ClearPendingEditEmployeeId();
            ClearPendingNavigation();
            SaveStateInSession();

            try
            {
                var actor = BuildAuditActor();
                var deleted = await _employeeManagementUseCase.DeleteAsync(id, actor, HttpContext.RequestAborted);
                if (deleted)
                {
                    TempData["SuccessMessage"] = "Empleado desactivado.";
                }
                else
                {
                    TempData["ErrorMessage"] = "El empleado no existe o ya estaba desactivado.";
                }
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Error al eliminar empleado");
                TempData["ErrorMessage"] = "No se pudo eliminar el empleado.";
            }

            return RedirectToCurrentState();
        }
    }
}
