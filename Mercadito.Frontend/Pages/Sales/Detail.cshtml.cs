using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Dtos.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class DetailModel(ISalesApiAdapter salesApiAdapter, ILogger<DetailModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public long SaleId { get; set; }

    public SaleDetailDto? Sale { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (SaleId <= 0)
        {
            return NotFound();
        }

        var result = await salesApiAdapter.GetSaleDetailAsync(SaleId, HttpContext.RequestAborted);
        if (result.Success && result.Data != null)
        {
            Sale = result.Data;
            return Page();
        }

        ErrorMessage = result.Errors.FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
            ?? "No se pudo cargar el detalle de la venta.";
        logger.LogWarning("No se pudo cargar el detalle de venta {SaleId}: {Message}", SaleId, ErrorMessage);
        return Page();
    }
}
