using Microsoft.AspNetCore.Mvc;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.suppliers.application.models;

namespace Mercadito.Pages.Suppliers
{
    public partial class SuppliersModel
    {
        public async Task<IActionResult> OnPostCreate([Bind(Prefix = "NewSupplier")] CreateSupplierDto newSupplier)
        {
            NewSupplier = newSupplier;
            LoadStateFromSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
                ShowCreateSupplierModal = true;
                await LoadSuppliersAsync();
                await LoadNextSupplierCodePreviewAsync();
                NewSupplier.Codigo = NextSupplierCodePreview;
                ModelState.Remove(string.Concat(nameof(NewSupplier), ".", nameof(NewSupplier.Codigo)));
                return Page();
            }

            var result = await _register.ExecuteAsync(NewSupplier);

            if (result.IsFailure)
            {
                ApplyResultErrors(result, nameof(NewSupplier));
                TempData["ErrorMessage"] = "Corrige los errores en el formulario.";
                ShowCreateSupplierModal = true;
                await LoadSuppliersAsync();
                await LoadNextSupplierCodePreviewAsync();
                NewSupplier.Codigo = NextSupplierCodePreview;
                ModelState.Remove(string.Concat(nameof(NewSupplier), ".", nameof(NewSupplier.Codigo)));
                return Page();
            }

            TempData["SuccessMessage"] = "Proveedor registrado exitosamente.";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostEdit([Bind(Prefix = "EditSupplier")] UpdateSupplierDto editSupplier)
        {
            EditSupplier = editSupplier;
            LoadStateFromSession();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
                ShowEditSupplierModal = true;
                await LoadSuppliersAsync();
                await LoadNextSupplierCodePreviewAsync();
                return Page();
            }

            var result = await _update.ExecuteAsync(EditSupplier);

            if (result.IsFailure)
            {
                ApplyResultErrors(result, nameof(EditSupplier));
                TempData["ErrorMessage"] = "Corrige los errores en el formulario.";
                ShowEditSupplierModal = true;
                await LoadSuppliersAsync();
                await LoadNextSupplierCodePreviewAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDelete(long id)
        {
            LoadStateFromSession();

            try
            {
                var result = await _delete.ExecuteAsync(id, HttpContext.RequestAborted);
                if (result.IsFailure || result.Value == 0)
                {
                    TempData["ErrorMessage"] = "El proveedor no existe o ya estaba desactivado.";
                    return RedirectToPage();
                }

                TempData["SuccessMessage"] = "Proveedor desactivado.";
                return RedirectToPage();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "No se pudo eliminar el proveedor {SupplierId}", id);
                TempData["ErrorMessage"] = "No se pudo eliminar el proveedor.";
                return RedirectToPage();
            }
        }
    }
}
