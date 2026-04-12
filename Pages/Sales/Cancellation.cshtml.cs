using Mercadito.Pages.Infrastructure;
using Mercadito.src.sales.application.models;
using Mercadito.src.sales.application.ports.input;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Pages.Sales
{
    public class CancellationModel(
        ISalesTransactionFacade salesTransactionFacade,
        ILogger<CancellationModel> logger) : AppPageModel
    {
        [BindProperty]
        public CancelSaleDto CancelRequest { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long DetailSaleId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = SalesTableSorting.DefaultSortBy;

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = SalesTableSorting.DefaultSortDirection;

        public IReadOnlyList<SaleSummaryItem> RecentSales { get; private set; } = [];
        public SaleDetailDto? SelectedSaleDetail { get; private set; }
        public bool ShowDetailModal { get; private set; }
        public bool ShowCancelModal { get; private set; }
        public int RegisteredSalesCount { get; private set; }
        public int CancelledSalesCount { get; private set; }
        public decimal CancelledAmountTotal { get; private set; }
        public string CancelTargetCode { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();

            if (DetailSaleId > 0)
            {
                await LoadSaleDetailAsync(DetailSaleId);
            }
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            var actor = BuildAuditActor();
            var result = await salesTransactionFacade.CancelAsync(CancelRequest, actor, HttpContext.RequestAborted);
            if (result.IsFailure)
            {
                ApplyResultErrors(result, nameof(CancelRequest));
                ShowCancelModal = true;
                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Venta anulada correctamente y stock restaurado.";
            return RedirectToPage(new { SortBy, SortDirection });
        }

        private async Task LoadPageDataAsync()
        {
            SortBy = NormalizeSortBy(SortBy);
            SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);

            var recentSalesResult = await salesTransactionFacade.GetRecentSalesAsync(
                30,
                SortBy,
                SortDirection,
                HttpContext.RequestAborted);
            if (recentSalesResult.IsFailure)
            {
                logger.LogError("No se pudo cargar el historial para anulación de ventas: {Message}", recentSalesResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el historial de ventas.";
                RecentSales = [];
                CancelTargetCode = string.Empty;
                return;
            }

            RecentSales = recentSalesResult.Value;

            var overviewMetricsResult = await salesTransactionFacade.GetOverviewMetricsAsync(HttpContext.RequestAborted);
            if (overviewMetricsResult.IsFailure)
            {
                logger.LogError("No se pudo cargar el resumen para anulación de ventas: {Message}", overviewMetricsResult.ErrorMessage);
                TempData["ErrorMessage"] = "No se pudo cargar el resumen de ventas.";
                RegisteredSalesCount = 0;
                CancelledSalesCount = 0;
                CancelledAmountTotal = 0m;
            }
            else
            {
                RegisteredSalesCount = overviewMetricsResult.Value.RegisteredSalesCount;
                CancelledSalesCount = overviewMetricsResult.Value.CancelledSalesCount;
                CancelledAmountTotal = overviewMetricsResult.Value.CancelledAmountTotal;
            }

            CancelTargetCode = ResolveSaleCode(CancelRequest.SaleId);
        }

        private string ResolveSaleCode(long saleId)
        {
            if (saleId <= 0)
            {
                return string.Empty;
            }

            foreach (var sale in RecentSales)
            {
                if (sale.Id == saleId)
                {
                    return sale.Code;
                }
            }

            return $"Venta #{saleId}";
        }

        private async Task LoadSaleDetailAsync(long saleId)
        {
            var result = await salesTransactionFacade.GetSaleDetailAsync(saleId, HttpContext.RequestAborted);
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
