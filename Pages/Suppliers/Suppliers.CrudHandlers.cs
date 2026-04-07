using Microsoft.AspNetCore.Mvc;
using Mercadito.src.suppliers.application.models;
namespace Mercadito.Pages.Suppliers
{
    public partial class SuppliersModel
    {
        public async Task<IActionResult> OnPostCreate()
        {
            LoadStateFromSession();

            var createDto = new CreateSupplierDto
            {
                Nombre = RazonSocial,
                Codigo = Codigo,
                Direccion = Direccion,
                Contacto = Contacto,
                Rubro = Rubro
            };

            var result =await _register.ExecuteAsync(createDto);

            if (result.IsFailure)
            {
                ApplyCreateErrors(result.Errors);

                TempData["ErrorMessage"] = "Corrige los errores en el formulario.";
                
                ShowModalOnError = true;
                ActiveModal = "create";
                await LoadSuppliersAsync();
                await LoadNextSupplierCodePreviewAsync();
                Codigo = NextSupplierCodePreview;
                return Page();
            }

            TempData["SuccessMessage"] = "Proveedor registrado exitosamente.";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostEdit()
        {
            LoadStateFromSession();

            var updateDto = new UpdateSupplierDto
            {
                Id = EditId,
                Nombre = EditRazonSocial,
                Codigo = EditCodigo,
                Direccion = EditDireccion,
                Contacto = EditContacto,
                Rubro = EditRubro,
                Telefono = EditTelefono
            };

            var result = await _update.ExecuteAsync(updateDto);

            if (result.IsFailure)
            {
                ApplyEditErrors(result.Errors);

                TempData["ErrorMessage"] = "Corrige los errores en el formulario.";
                
                ShowModalOnError = true;
                ActiveModal = "edit";
                await LoadSuppliersAsync();
                await LoadNextSupplierCodePreviewAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
            return RedirectToPage();
        }
    }
}
