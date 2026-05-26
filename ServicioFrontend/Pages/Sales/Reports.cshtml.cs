using System.Globalization;
using ServicioFrontend.Adapters.Sales;
using ServicioFrontend.Dtos.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ServicioFrontend.Pages.Sales;

public sealed class ReportsModel(
    ISalesApiAdapter salesApiAdapter,
    IDailyCashClosingExcelExporter excelExporter,
    ISalesListingExcelExporter salesListingExcelExporter,
    ILogger<ReportsModel> logger) : PageModel
{
    private static readonly IReadOnlyList<string> KnownStatuses = ["Registrada", "Anulada"];
    private static readonly IReadOnlyList<string> KnownPaymentMethods = ["Efectivo", "Tarjeta", "QR"];

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = SalesTableSorting.DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = SalesTableSorting.DefaultSortDirection;

    [BindProperty(SupportsGet = true)]
    public DateOnly BusinessDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [BindProperty(SupportsGet = true)]
    public DateOnly? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string PaymentMethodFilter { get; set; } = string.Empty;

    public IReadOnlyList<string> StatusOptions => KnownStatuses;
    public IReadOnlyList<string> PaymentMethodOptions => KnownPaymentMethods;
    public IReadOnlyList<SaleSummaryDto> RecentSales { get; private set; } = [];
    public IReadOnlyList<string> Errors { get; private set; } = [];
    public DailyCashClosingReportDto? DailyCashClosing { get; private set; }
    public int RegisteredSalesCount { get; private set; }
    public int CancelledSalesCount { get; private set; }
    public decimal RegisteredAmountTotal { get; private set; }
    public decimal ListingSalesTotal => RecentSales.Sum(sale => sale.Total);
    public DateTime ListingGeneratedAt { get; private set; } = DateTime.Now;

    public async Task OnGetAsync()
    {
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);
        BusinessDate = BusinessDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : BusinessDate;
        StatusFilter = NormalizeOption(StatusFilter, KnownStatuses);
        PaymentMethodFilter = NormalizeOption(PaymentMethodFilter, KnownPaymentMethods);
        ListingGeneratedAt = DateTime.Now;

        var errors = new List<string>();

        var metricsResult = await salesApiAdapter.GetMetricsAsync(HttpContext.RequestAborted);
        if (metricsResult.Success && metricsResult.Data != null)
        {
            RegisteredSalesCount = metricsResult.Data.RegisteredSales;
            CancelledSalesCount = metricsResult.Data.CancelledSales;
            RegisteredAmountTotal = metricsResult.Data.RegisteredAmount;
        }
        else
        {
            errors.AddRange(metricsResult.Errors);
            logger.LogWarning("No se pudo cargar el resumen de reportes: {Errors}", string.Join(" | ", metricsResult.Errors));
        }

        var salesResult = await salesApiAdapter.GetRecentSalesAsync(
            100,
            SortBy,
            SortDirection,
            FromDate,
            ToDate,
            StatusFilter,
            PaymentMethodFilter,
            HttpContext.RequestAborted);
        if (salesResult.Success && salesResult.Data != null)
        {
            RecentSales = salesResult.Data;
        }
        else
        {
            errors.AddRange(salesResult.Errors);
            logger.LogWarning("No se pudo cargar el historial de reportes: {Errors}", string.Join(" | ", salesResult.Errors));
        }

        var closingResult = await salesApiAdapter.GetDailyCashClosingReportAsync(BusinessDate, HttpContext.RequestAborted);
        if (closingResult.Success && closingResult.Data != null)
        {
            DailyCashClosing = closingResult.Data;
        }
        else
        {
            errors.AddRange(closingResult.Errors);
            logger.LogWarning("No se pudo cargar el cierre diario de caja: {Errors}", string.Join(" | ", closingResult.Errors));
        }

        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IActionResult> OnGetExportDailyCashClosingExcelAsync(DateOnly? businessDate)
    {
        var reportDate = businessDate ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await salesApiAdapter.GetDailyCashClosingReportAsync(reportDate, HttpContext.RequestAborted);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = "No se pudo generar el Excel del cierre diario.";
            return RedirectToPage(new
            {
                businessDate = reportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                sortBy = SortBy,
                sortDirection = SortDirection,
                fromDate = FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                toDate = ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                statusFilter = StatusFilter,
                paymentMethodFilter = PaymentMethodFilter
            });
        }

        var fileBytes = excelExporter.Export(result.Data);
        var fileName = $"cierre-caja-{reportDate:yyyy-MM-dd}.xlsx";

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public async Task<IActionResult> OnGetExportSalesListingExcelAsync()
    {
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);
        StatusFilter = NormalizeOption(StatusFilter, KnownStatuses);
        PaymentMethodFilter = NormalizeOption(PaymentMethodFilter, KnownPaymentMethods);

        var result = await salesApiAdapter.GetRecentSalesAsync(
            500,
            SortBy,
            SortDirection,
            FromDate,
            ToDate,
            StatusFilter,
            PaymentMethodFilter,
            HttpContext.RequestAborted);

        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = "No se pudo generar el Excel del listado de ventas.";
            return RedirectToPage(new
            {
                businessDate = BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                sortBy = SortBy,
                sortDirection = SortDirection,
                fromDate = FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                toDate = ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                statusFilter = StatusFilter,
                paymentMethodFilter = PaymentMethodFilter
            });
        }

        var context = new SalesListingExcelExportContext(
            FromDate,
            ToDate,
            StatusFilter,
            PaymentMethodFilter,
            DateTime.Now);
        var fileBytes = salesListingExcelExporter.Export(result.Data, context);
        var fileName = $"reporte-ventas-{DateTime.Now:yyyy-MM-dd-HHmm}.xlsx";

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public string GetSortIcon(string columnName)
    {
        return SalesTableSorting.GetSortIcon(SortBy, SortDirection, NormalizeSortBy(columnName));
    }

    public string GetNextSortDirection(string columnName)
    {
        return SalesTableSorting.GetNextSortDirection(SortBy, SortDirection, NormalizeSortBy(columnName));
    }

    public string GetDailyProductSharePercent(DailyProductSalesSummaryDto product)
    {
        if (DailyCashClosing == null || DailyCashClosing.RegisteredAmountTotal <= 0)
        {
            return "0";
        }

        var percentage = product.TotalAmount / DailyCashClosing.RegisteredAmountTotal * 100;
        percentage = Math.Clamp(percentage, 0, 100);
        return percentage.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public string GetFilterSummary()
    {
        var fromDate = FromDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Todos";
        var toDate = ToDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Todos";
        var status = string.IsNullOrWhiteSpace(StatusFilter) ? "Todos" : StatusFilter;
        var paymentMethod = string.IsNullOrWhiteSpace(PaymentMethodFilter) ? "Todos" : PaymentMethodFilter;
        return $"Desde: {fromDate} · Hasta: {toDate} · Estado: {status} · Método: {paymentMethod}";
    }

    private static string NormalizeSortBy(string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

        return normalizedValue switch
        {
            "code" or "createdat" or "customer" or "paymentmethod" or "total" or "status" => normalizedValue,
            _ => SalesTableSorting.DefaultSortBy
        };
    }

    private static string NormalizeOption(string? value, IReadOnlyList<string> knownValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        foreach (var knownValue in knownValues)
        {
            if (string.Equals(value.Trim(), knownValue, StringComparison.OrdinalIgnoreCase))
            {
                return knownValue;
            }
        }

        return string.Empty;
    }
}
