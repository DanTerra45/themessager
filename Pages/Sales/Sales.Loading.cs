using Mercadito.src.domain.shared.validation;

namespace Mercadito.Pages.Sales
{
    public partial class SalesModel
    {
        private async Task LoadEditDraftAsync(long saleId)
        {
            var result = await _salesQueryFacade.GetSaleDetailAsync(saleId, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                EditSaleId = 0;
                OriginalSaleLineCreditsJson = "{}";
                return;
            }

            if (string.Equals(result.Value.Status, "Anulada", StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "No se puede editar una venta anulada.";
                EditSaleId = 0;
                OriginalSaleLineCreditsJson = "{}";
                return;
            }

            ApplyDraftFromSaleDetail(result.Value);
        }

        private async Task LoadPageDataAsync()
        {
            SortBy = NormalizeSortBy(SortBy);
            SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);
            CustomerSearchTerm = ValidationText.NormalizeTrimmed(CustomerSearchTerm);
            ProductSearchTerm = ValidationText.NormalizeTrimmed(ProductSearchTerm);

            var contextResult = await _salesQueryFacade.LoadRegistrationContextAsync(
                customerSearchTerm: CustomerSearchTerm,
                productSearchTerm: ProductSearchTerm,
                cancellationToken: HttpContext.RequestAborted);

            if (contextResult.IsFailure)
            {
                _logger.LogError("No se pudo cargar el contexto de ventas: {Message}", contextResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el contexto de ventas.";
                RegistrationContext = new();
            }
            else
            {
                RegistrationContext = contextResult.Value;
            }

            var recentSalesResult = await _salesQueryFacade.GetRecentSalesAsync(
                20,
                SortBy,
                SortDirection,
                HttpContext.RequestAborted);
            if (recentSalesResult.IsFailure)
            {
                _logger.LogError("No se pudo cargar el historial reciente de ventas: {Message}", recentSalesResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el historial reciente de ventas.";
                RecentSales = [];
            }
            else
            {
                RecentSales = recentSalesResult.Value;
            }

            var overviewMetricsResult = await _salesQueryFacade.GetOverviewMetricsAsync(HttpContext.RequestAborted);
            if (overviewMetricsResult.IsFailure)
            {
                _logger.LogError("No se pudo cargar el resumen general de ventas: {Message}", overviewMetricsResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el resumen general de ventas.";
                SalesTodayCount = 0;
                SalesTodayTotal = 0m;
                AverageTicketToday = 0m;
            }
            else
            {
                SalesTodayCount = overviewMetricsResult.Value.SalesTodayCount;
                SalesTodayTotal = overviewMetricsResult.Value.SalesTodayTotal;
                AverageTicketToday = overviewMetricsResult.Value.AverageTicketToday;
            }

            BuildDraftPresentation();
            ShowNewCustomerPanel = ShouldShowNewCustomerPanel();
            AutoOpenReceiptUrl = BuildReceiptUrl(AutoOpenReceiptSaleId);
        }

        private async Task LoadSaleDetailAsync(long saleId)
        {
            var result = await _salesQueryFacade.GetSaleDetailAsync(saleId, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return;
            }

            SelectedSaleDetail = result.Value;
            ShowDetailModal = true;
        }
    }
}
