using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Sales
{
    public class ReceiptModel(
        ISalesTransactionFacade salesTransactionFacade,
        ILogger<ReceiptModel> logger) : PageModel
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

            var result = await salesTransactionFacade.GetSaleReceiptAsync(SaleId, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                logger.LogWarning("No se pudo cargar el comprobante de venta {SaleId}: {Message}", SaleId, result.ErrorMessage);
                return NotFound();
            }

            Receipt = result.Value;
            return Page();
        }
    }
}
