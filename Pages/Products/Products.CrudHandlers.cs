using Mercadito.src.products.domain.dto;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
                StorePendingValidationErrors(PendingCreateErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _productManagementUseCase.CreateAsync(NewProduct, HttpContext.RequestAborted);

                if (IsRecentOrderPreset(OrderPreset))
                {
                    CurrentPage = 1;
                }

                TempData["SuccessMessage"] = "Producto agregado exitosamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al crear producto");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingCreateModal(NewProduct);
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
                StorePendingValidationErrors(PendingEditErrorsSessionKey);
                return RedirectToCurrentState();
            }

            try
            {
                await _productManagementUseCase.UpdateAsync(EditProduct, HttpContext.RequestAborted);

                TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                return RedirectToCurrentState();
            }
            catch (ValidationException validationException)
            {
                _logger.LogWarning(validationException, "Validación de negocio al actualizar producto");
                TempData["ErrorMessage"] = validationException.Message;
                StorePendingEditModal(EditProduct);
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
                var wasDeleted = await _productManagementUseCase.DeleteAsync(id, HttpContext.RequestAborted);
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
