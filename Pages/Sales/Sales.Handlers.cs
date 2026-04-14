using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared.validation;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Sales
{
    public partial class SalesModel
    {
        public async Task<IActionResult> OnGetSearchCustomersAsync(string customerSearchTerm)
        {
            var normalizedTerm = ValidationText.NormalizeTrimmed(customerSearchTerm);
            var contextResult = await _salesTransactionFacade.LoadRegistrationContextAsync(
                customerSearchTerm: normalizedTerm,
                cancellationToken: HttpContext.RequestAborted);

            if (contextResult.IsFailure)
            {
                _logger.LogError("No se pudo buscar clientes para ventas: {Message}", contextResult.ErrorMessage);
                return StatusCode(500, Array.Empty<CustomerLookupItem>());
            }

            return new JsonResult(contextResult.Value.Customers);
        }

        public async Task<IActionResult> OnGetSearchProductsAsync(string productSearchTerm)
        {
            var normalizedTerm = ValidationText.NormalizeTrimmed(productSearchTerm);
            var contextResult = await _salesTransactionFacade.LoadRegistrationContextAsync(
                productSearchTerm: normalizedTerm,
                cancellationToken: HttpContext.RequestAborted);

            if (contextResult.IsFailure)
            {
                _logger.LogError("No se pudo buscar productos para ventas: {Message}", contextResult.ErrorMessage);
                return StatusCode(500, Array.Empty<SaleProductOption>());
            }

            return new JsonResult(contextResult.Value.Products);
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            EnsureDraftDefaults();
            ShowCreateModal = true;
            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            EnsureDraftDefaults();
            await LoadPageDataAsync();

            var actor = BuildAuditActor();
            var result = await _salesTransactionFacade.RegisterAsync(SaleDraft, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result, nameof(SaleDraft));
                ShowCreateModal = true;
                BuildDraftPresentation();
                return Page();
            }

            TempData["SuccessMessage"] = $"Venta {result.Value.Code} registrada correctamente.";
            return RedirectToPage(new { AutoOpenReceiptSaleId = result.Value.Id });
        }
    }
}
