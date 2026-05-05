using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Dtos.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class ReceiptModel(ISalesApiAdapter salesApiAdapter, ILogger<ReceiptModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public long SaleId { get; set; }

    public SaleReceiptDto? Receipt { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (SaleId <= 0)
        {
            return NotFound();
        }

        var result = await salesApiAdapter.GetSaleReceiptAsync(SaleId, HttpContext.RequestAborted);
        if (!result.Success || result.Data == null)
        {
            logger.LogWarning("No se pudo cargar el comprobante de venta {SaleId}: {Errors}", SaleId, string.Join(" | ", result.Errors));
            return NotFound();
        }

        Receipt = result.Data;
        return Page();
    }
}
