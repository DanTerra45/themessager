using Mercadito.src.categories.application.models;
using Microsoft.AspNetCore.Mvc;
using Mercadito.src.shared.domain.exceptions;

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
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);

            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewCategory);
                StoreModelStateErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                var actor = BuildAuditActor();
                var result = await _categoryManagementUseCase.CreateAsync(NewCategory, actor, HttpContext.RequestAborted);
                if (result.IsFailure)
                {
                    if (result.Errors.Count > 0)
                    {
                        ApplyValidationErrors("NewCategory", result.Errors);
                        StoreModelStateErrors(PendingCreateErrorsSessionKey);
                        TempData["ErrorMessage"] = "Corrige los errores del formulario.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    StorePendingCreateModal(NewCategory);
                    return RedirectToCurrentState();
                }

                _logger.LogInformation("Categoría creada: {Name}", NewCategory.Name);

                TempData["SuccessMessage"] = "Categoría agregada exitosamente.";
                return RedirectToCurrentState();
            }
            catch (DataStoreUnavailableException exception)
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
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditCategory);
                StoreModelStateErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                var actor = BuildAuditActor();
                var result = await _categoryManagementUseCase.UpdateAsync(EditCategory, actor, HttpContext.RequestAborted);
                if (result.IsFailure)
                {
                    if (result.Errors.Count > 0)
                    {
                        ApplyValidationErrors("EditCategory", result.Errors);
                        StoreModelStateErrors(PendingEditErrorsSessionKey);
                        TempData["ErrorMessage"] = "Corrige los errores del formulario.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    StorePendingEditModal(EditCategory);
                    return RedirectToCurrentState();
                }

                _logger.LogInformation("Categoría actualizada: {Id}", EditCategory.Id);
                TempData["SuccessMessage"] = "Categoría actualizada correctamente.";
                return RedirectToCurrentState();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Error al actualizar categoría");
                TempData["ErrorMessage"] = "Error al actualizar la categoría. Intente nuevamente.";
                StorePendingEditModal(EditCategory);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id, string sortBy = "", string sortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);

            ClearPendingEditCategoryId();
            ClearPendingNavigation();
            SaveStateInSession();

            try
            {
                var actor = BuildAuditActor();
                var wasDeleted = await _categoryManagementUseCase.DeleteAsync(id, actor, HttpContext.RequestAborted);
                if (wasDeleted)
                {
                    TempData["SuccessMessage"] = "Categoría desactivada.";
                }
                else
                {
                    TempData["ErrorMessage"] = "La categoría no existe o ya estaba desactivada.";
                }
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Error al eliminar la categoría");
                TempData["ErrorMessage"] = "No se pudo eliminar la categoría.";
            }

            return RedirectToCurrentState();
        }
    }
}
