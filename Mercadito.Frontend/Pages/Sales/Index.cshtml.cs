using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Dtos.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class IndexModel(ISalesApiAdapter salesApiAdapter, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = SalesTableSorting.DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = SalesTableSorting.DefaultSortDirection;

    public SalesMetricsDto Metrics { get; private set; } = new(0, 0, 0m, 0m, 0, 0m, 0m);

    public IReadOnlyList<SaleSummaryDto> RecentSales { get; private set; } = [];

    public IReadOnlyList<string> Errors { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SortBy = SalesTableSorting.NormalizeSortBy(SortBy);
        SortDirection = SalesTableSorting.NormalizeSortDirection(SortDirection);

        var errors = new List<string>();
        var metricsResult = await salesApiAdapter.GetMetricsAsync(HttpContext.RequestAborted);
        if (metricsResult.Success && metricsResult.Data != null)
        {
            Metrics = metricsResult.Data;
        }
        else
        {
            errors.AddRange(metricsResult.Errors);
            logger.LogWarning("No se pudieron cargar las métricas de ventas: {Errors}", string.Join(" | ", metricsResult.Errors));
        }

        var salesResult = await salesApiAdapter.GetRecentSalesAsync(20, SortBy, SortDirection, HttpContext.RequestAborted);
        if (salesResult.Success && salesResult.Data != null)
        {
            RecentSales = salesResult.Data;
        }
        else
        {
            errors.AddRange(salesResult.Errors);
            logger.LogWarning("No se pudieron cargar las ventas recientes: {Errors}", string.Join(" | ", salesResult.Errors));
        }

        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public string GetSortIcon(string columnName)
    {
        return SalesTableSorting.GetSortIcon(SortBy, SortDirection, SalesTableSorting.NormalizeSortBy(columnName));
    }

    public string GetNextSortDirection(string columnName)
    {
        return SalesTableSorting.GetNextSortDirection(SortBy, SortDirection, SalesTableSorting.NormalizeSortBy(columnName));
    }
}
