using Mercadito.src.shared.domain.validation;

namespace Mercadito.Pages.Sales
{
    public partial class SalesModel
    {
        private async Task LoadPageDataAsync()
        {
            SortBy = NormalizeSortBy(SortBy);
            SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);
            CustomerSearchTerm = ValidationText.NormalizeTrimmed(CustomerSearchTerm);
            ProductSearchTerm = ValidationText.NormalizeTrimmed(ProductSearchTerm);

            var contextResult = await _salesTransactionFacade.LoadRegistrationContextAsync(
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

            var recentSalesResult = await _salesTransactionFacade.GetRecentSalesAsync(
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

            var overviewMetricsResult = await _salesTransactionFacade.GetOverviewMetricsAsync(HttpContext.RequestAborted);
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
            var result = await _salesTransactionFacade.GetSaleDetailAsync(saleId, HttpContext.RequestAborted);
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
