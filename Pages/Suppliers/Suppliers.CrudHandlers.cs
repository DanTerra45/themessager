using Microsoft.AspNetCore.Mvc;
using Mercadito.src.domain.provedores.dto;
namespace Mercadito.Pages.Suppliers
{
    public partial class SuppliersModel
    {
        public async Task<IActionResult> OnPostCreate()
        {
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
                if (result.Errors.TryGetValue("Nombre", out var nombreErrors))
                    RazonSocialErrors = nombreErrors;
                if (result.Errors.TryGetValue("Codigo", out var codigoErrors))
                    CodigoErrors = codigoErrors;
                if (result.Errors.TryGetValue("Direccion", out var dirErrors))
                    DireccionErrors = dirErrors;
                if (result.Errors.TryGetValue("Contacto", out var contErrors))
                    ContactoErrors = contErrors;
                if (result.Errors.TryGetValue("Rubro", out var rubroErrors))
                    RubroErrors = rubroErrors;

                TempData["ErrorMessage"] = "Corrige los errores en el formulario.";
                
                ShowModalOnError = true;
                ActiveModal = "create";
                LoadSuppliersStub();
                return Page();
            }

            TempData["SuccessMessage"] = "Proveedor registrado exitosamente.";
            
            await SaveSupplierStub(createDto);
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostEdit()
        {
            var updateDto = new UpdateSupplierDto
            {
                Id = EditId,
                Nombre = EditRazonSocial,
                Codigo = EditCodigo,
                Direccion = EditDireccion,
                Contacto = EditContacto,
                Rubro = EditRubro
            };

            var result = _updateValidator.Validate(updateDto);

            if (result.IsFailure)
            {
                if (result.Errors.TryGetValue("Nombre", out var nombreErrors))
                    EditRazonSocialErrors = nombreErrors;
                if (result.Errors.TryGetValue("Codigo", out var codigoErrors))
                    EditCodigoErrors = codigoErrors;
                if (result.Errors.TryGetValue("Direccion", out var dirErrors))
                    EditDireccionErrors = dirErrors;
                if (result.Errors.TryGetValue("Contacto", out var contErrors))
                    EditContactoErrors = contErrors;
                if (result.Errors.TryGetValue("Rubro", out var rubroErrors))
                    EditRubroErrors = rubroErrors;

                TempData["ErrorMessage"] = "Corrige los errores en el formulario.";
                
                ShowModalOnError = true;
                ActiveModal = "edit";
                LoadSuppliersStub();
                return Page();
            }

            TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
            
            await UpdateSupplierStub(updateDto);
            return RedirectToPage();
        }
    }
}