using System.ComponentModel.DataAnnotations;
using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Dtos.Sales;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class CancelModel(ISalesApiAdapter salesApiAdapter, ILogger<CancelModel> logger) : FrontendPageModel
{
    [BindProperty(SupportsGet = true)]
    public long SaleId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(200, ErrorMessage = "El motivo no puede exceder 200 caracteres.")]
    public string Reason { get; set; } = string.Empty;

    public SaleDetailDto? Sale { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadSaleOrNotFoundAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (SaleId <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadSaleAsync();
            return Page();
        }

        var result = await salesApiAdapter.CancelSaleAsync(
            SaleId,
            Reason,
            BuildActorContext(),
            HttpContext.RequestAborted);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Venta anulada correctamente.";
            return RedirectToPage("/Sales/Index");
        }

        ErrorMessage = result.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
            ?? "No se pudo anular la venta.";
        logger.LogWarning("No se pudo anular la venta {SaleId}: {Message}", SaleId, ErrorMessage);

        await LoadSaleAsync();
        return Page();
    }

    private async Task<IActionResult> LoadSaleOrNotFoundAsync()
    {
        if (SaleId <= 0)
        {
            return NotFound();
        }

        await LoadSaleAsync();
        if (Sale == null)
        {
            return NotFound();
        }

        return Page();
    }

    private async Task LoadSaleAsync()
    {
        var result = await salesApiAdapter.GetSaleDetailAsync(SaleId, HttpContext.RequestAborted);
        if (result.Success && result.Data != null)
        {
            Sale = result.Data;
            return;
        }

        ErrorMessage = result.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
            ?? "No se pudo cargar la venta.";
    }
}
