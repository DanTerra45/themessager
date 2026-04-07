using Mercadito.src.products.application.models;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Products
{
    public partial class ProductsModel
    {
        public async Task<IActionResult> OnPostCreateAsync(
            [Bind(Prefix = "NewProduct")] CreateProductDto newProduct,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            NewProduct = newProduct;
            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);

            ClearPendingEditProductId();
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido al crear producto");
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                StorePendingCreateModal(NewProduct);
                StoreModelStateErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                var actor = BuildAuditActor();
                var result = await _productManagementUseCase.CreateAsync(NewProduct, actor, HttpContext.RequestAborted);
                if (result.IsFailure)
                {
                    if (result.Errors.Count > 0)
                    {
                        ApplyValidationErrors("NewProduct", result.Errors);
                        StoreModelStateErrors(PendingCreateErrorsSessionKey);
                        TempData["ErrorMessage"] = "Corrige los errores del formulario.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    StorePendingCreateModal(NewProduct);
                    return RedirectToCurrentState();
                }

                if (IsRecentOrderPreset(OrderPreset))
                {
                    CurrentPage = 1;
                }

                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al crear producto");
                TempData["ErrorMessage"] = "Error al guardar el producto. Intente nuevamente.";
                StorePendingCreateModal(NewProduct);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(
            [Bind(Prefix = "EditProduct")] UpdateProductDto editProduct,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            EditProduct = editProduct;
            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);
            ClearPendingNavigation();
            SaveStateInSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                StorePendingEditModal(EditProduct);
                StoreModelStateErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                var actor = BuildAuditActor();
                var result = await _productManagementUseCase.UpdateAsync(EditProduct, actor, HttpContext.RequestAborted);
                if (result.IsFailure)
                {
                    if (result.Errors.Count > 0)
                    {
                        ApplyValidationErrors("EditProduct", result.Errors);
                        StoreModelStateErrors(PendingEditErrorsSessionKey);
                        TempData["ErrorMessage"] = "Corrige los errores del formulario.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }

                    StorePendingEditModal(EditProduct);
                    return RedirectToCurrentState();
                }

                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al actualizar producto");
                TempData["ErrorMessage"] = "Error al actualizar el producto. Intente nuevamente.";
                StorePendingEditModal(EditProduct);
                return RedirectToCurrentState();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            long id,
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "")
        {
            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);
            ClearPendingNavigation();
            SaveStateInSession();

            try
            {
                var actor = BuildAuditActor();
                var wasDeleted = await _productManagementUseCase.DeleteAsync(id, actor, HttpContext.RequestAborted);
                if (wasDeleted)
                {
                    TempData["SuccessMessage"] = "Producto desactivado.";
                }
                else
                {
                    TempData["ErrorMessage"] = "El producto no existe o ya estaba desactivado.";
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al eliminar producto");
                TempData["ErrorMessage"] = "No se pudo eliminar el producto.";
            }

            return RedirectToCurrentState();
        }
    }
}


