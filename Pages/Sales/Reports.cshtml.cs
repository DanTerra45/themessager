using Mercadito.Pages.Infrastructure;
using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Sales
{
    public class ReportsModel(
        ISalesQueryFacade salesQueryFacade,
        ILogger<ReportsModel> logger) : AppPageModel
    {
        [BindProperty(SupportsGet = true)]
        public long DetailSaleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = SalesTableSorting.DefaultSortBy;

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = SalesTableSorting.DefaultSortDirection;

        public IReadOnlyList<SaleSummaryItem> RecentSales { get; private set; } = [];
        public SaleDetailDto? SelectedSaleDetail { get; private set; }
        public bool ShowDetailModal { get; private set; }
        public int RegisteredSalesCount { get; private set; }
        public int CancelledSalesCount { get; private set; }
        public decimal RegisteredAmountTotal { get; private set; }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();

            if (DetailSaleId > 0)
            {
                await LoadSaleDetailAsync(DetailSaleId);
            }
        }

        private async Task LoadPageDataAsync()
        {
            SortBy = NormalizeSortBy(SortBy);
            SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);

            var recentSalesResult = await salesQueryFacade.GetRecentSalesAsync(
                30,
                SortBy,
                SortDirection,
                HttpContext.RequestAborted);
            if (recentSalesResult.IsFailure)
            {
                logger.LogError("No se pudo cargar el historial de ventas para reportes: {Message}", recentSalesResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el historial de ventas para reportes.";
                RecentSales = [];
                return;
            }

            RecentSales = recentSalesResult.Value;

            var overviewMetricsResult = await salesQueryFacade.GetOverviewMetricsAsync(HttpContext.RequestAborted);
            if (overviewMetricsResult.IsFailure)
            {
                logger.LogError("No se pudo cargar el resumen para reportes de ventas: {Message}", overviewMetricsResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el resumen de ventas.";
                RegisteredSalesCount = 0;
                CancelledSalesCount = 0;
                RegisteredAmountTotal = 0m;
            }
            else
            {
                RegisteredSalesCount = overviewMetricsResult.Value.RegisteredSalesCount;
                CancelledSalesCount = overviewMetricsResult.Value.CancelledSalesCount;
                RegisteredAmountTotal = overviewMetricsResult.Value.RegisteredAmountTotal;
            }
        }

        private async Task LoadSaleDetailAsync(long saleId)
        {
            var result = await salesQueryFacade.GetSaleDetailAsync(saleId, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return;
            }

            SelectedSaleDetail = result.Value;
            ShowDetailModal = true;
        }

        public string GetSortIcon(string columnName)
        {
            return SalesTableSorting.GetSortIcon(SortBy, SortDirection, NormalizeSortBy(columnName));
        }

        public string GetNextSortDirection(string columnName)
        {
            return SalesTableSorting.GetNextSortDirection(SortBy, SortDirection, NormalizeSortBy(columnName));
        }

        private static string NormalizeSortBy(string? value)
        {
            return SalesTableSorting.NormalizeSortBy(
                value,
                "code",
                "createdat",
                "customer",
                "paymentmethod",
                "total",
                "status");
        }

    }
}
