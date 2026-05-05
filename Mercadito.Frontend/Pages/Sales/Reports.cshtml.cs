using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Dtos.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class ReportsModel(ISalesApiAdapter salesApiAdapter, ILogger<ReportsModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = SalesTableSorting.DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = SalesTableSorting.DefaultSortDirection;

    public IReadOnlyList<SaleSummaryDto> RecentSales { get; private set; } = [];
    public IReadOnlyList<string> Errors { get; private set; } = [];
    public int RegisteredSalesCount { get; private set; }
    public int CancelledSalesCount { get; private set; }
    public decimal RegisteredAmountTotal { get; private set; }

    public async Task OnGetAsync()
    {
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);

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

        var salesResult = await salesApiAdapter.GetRecentSalesAsync(30, SortBy, SortDirection, HttpContext.RequestAborted);
        if (salesResult.Success && salesResult.Data != null)
        {
            RecentSales = salesResult.Data;
        }
        else
        {
            errors.AddRange(salesResult.Errors);
            logger.LogWarning("No se pudo cargar el historial de reportes: {Errors}", string.Join(" | ", salesResult.Errors));
        }

        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();
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
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

        return normalizedValue switch
        {
            "code" or "createdat" or "customer" or "paymentmethod" or "total" or "status" => normalizedValue,
            _ => SalesTableSorting.DefaultSortBy
        };
    }
}
