using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.validation;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Sales
{
    public partial class SalesModel
    {
        public async Task<IActionResult> OnGetSearchCustomersAsync(string customerSearchTerm)
        {
            var normalizedTerm = ValidationText.NormalizeTrimmed(customerSearchTerm);
            var contextResult = await _salesQueryFacade.SearchCustomersAsync(
                normalizedTerm,
                HttpContext.RequestAborted);

            if (contextResult.IsFailure)
            {
                _logger.LogError("No se pudo buscar clientes para ventas: {Message}", contextResult.ErrorMessage);
                return StatusCode(500, Array.Empty<CustomerLookupItem>());
            }

            return new JsonResult(contextResult.Value);
        }

        public async Task<IActionResult> OnGetSearchProductsAsync(string productSearchTerm)
        {
            var normalizedTerm = ValidationText.NormalizeTrimmed(productSearchTerm);
            var contextResult = await _salesQueryFacade.SearchProductsAsync(
                normalizedTerm,
                HttpContext.RequestAborted);

            if (contextResult.IsFailure)
            {
                _logger.LogError("No se pudo buscar productos para ventas: {Message}", contextResult.ErrorMessage);
                return StatusCode(500, Array.Empty<SaleProductOption>());
            }

            return new JsonResult(contextResult.Value);
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
            Result<SaleReceiptDto> result;
            if (EditSaleId > 0)
            {
                result = await _updateSaleFacade.UpdateAsync(
                    new UpdateSaleDto
                    {
                        SaleId = EditSaleId,
                        CustomerId = SaleDraft.CustomerId,
                        NewCustomer = SaleDraft.NewCustomer,
                        Channel = SaleDraft.Channel,
                        PaymentMethod = SaleDraft.PaymentMethod,
                        Lines = SaleDraft.Lines
                    },
                    actor,
                    HttpContext.RequestAborted);
            }
            else
            {
                result = await _registerSaleFacade.RegisterAsync(SaleDraft, actor, HttpContext.RequestAborted);
            }

            if (result.IsFailure)
            {
                ApplyResultErrors(result, nameof(SaleDraft));
                ShowCreateModal = true;
                BuildDraftPresentation();
                return Page();
            }

            TempData["SuccessMessage"] = $"Venta {result.Value.Code} {DraftSuccessActionText} correctamente.";
            return RedirectToPage(new { AutoOpenReceiptSaleId = result.Value.Id, SortBy, SortDirection });
        }
    }
}
