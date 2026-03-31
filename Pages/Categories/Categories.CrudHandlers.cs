using Mercadito.src.categories.domain.dto;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel
    {
        public async Task<IActionResult> OnPostCreateAsync(
            [Bind(Prefix = "NewCategory")] CreateCategoryDto newCategory,
            string sortBy = "",
            string sortDirection = "")
        {
            NewCategory = newCategory;
            SetSortState(sortBy, sortDirection);

            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewCategory);
                StorePendingValidationErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _categoryManagementUseCase.CreateAsync(NewCategory, HttpContext.RequestAborted);
                _logger.LogInformation("Categoría creada: {Name}", NewCategory.Name);

                TempData["SuccessMessage"] = "Categoría agregada exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al crear categoría");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingCreateModal(NewCategory);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear categoría");
                TempData["ErrorMessage"] = "Error al guardar la categoría. Intente nuevamente.";
                StorePendingCreateModal(NewCategory);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(
            [Bind(Prefix = "EditCategory")] UpdateCategoryDto editCategory,
            string sortBy = "",
            string sortDirection = "")
        {
            EditCategory = editCategory;
            SetSortState(sortBy, sortDirection);
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditCategory);
                StorePendingValidationErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _categoryManagementUseCase.UpdateAsync(EditCategory, HttpContext.RequestAborted);
                _logger.LogInformation("Categoría actualizada: {Id}", EditCategory.Id);
                TempData["SuccessMessage"] = "Categoría actualizada correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar categoría");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingEditModal(EditCategory);
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar categoría");
                TempData["ErrorMessage"] = "Error al actualizar la categoría. Intente nuevamente.";
                StorePendingEditModal(EditCategory);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, string sortBy = "", string sortDirection = "")
        {
            SetSortState(sortBy, sortDirection);

            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();

            try
            {
                var wasDeleted = await _categoryManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
                if (wasDeleted)
                {
                    TempData["SuccessMessage"] = "Categoría desactivada.";
                }
                else
                {
                    TempData["ErrorMessage"] = "La categoría no existe o ya estaba desactivada.";
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al eliminar la categoría");
                TempData["ErrorMessage"] = "No se pudo eliminar la categoría.";
            }

            return RedirectToCurrentState();
        }
    }
}
