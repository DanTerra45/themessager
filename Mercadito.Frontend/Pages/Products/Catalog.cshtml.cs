using Mercadito.Frontend.Adapters.Products;
using Mercadito.Frontend.Dtos.Categories;
using Mercadito.Frontend.Dtos.Products;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Products;

public sealed class CatalogModel(
    IProductsApiAdapter productsApiAdapter,
    IConfiguration configuration,
    ILogger<CatalogModel> logger) : FrontendPageModel, IProductListingPageModel
{
    private const string DefaultSortBy = "name";
    private const string DefaultSortDirection = "asc";
    private const string OrderPresetRecent = "recent";
    private const string OrderPresetAlphabeticalAsc = "az";
    private const string OrderPresetAlphabeticalDesc = "za";
    private const string OrderPresetCustom = "custom";

    private readonly int _defaultPageSize = ResolveDefaultPageSize(configuration);

    public IReadOnlyList<ProductDto> Products { get; private set; } = [];
    public IReadOnlyList<CategoryDto> Categories { get; private set; } = [];
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }
    public string OrderPreset { get; private set; } = OrderPresetAlphabeticalAsc;

    [BindProperty(SupportsGet = true)]
    public long CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public long CurrentAnchorProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = DefaultSortBy;

    [BindProperty(SupportsGet = true)]
    public string SortDirection { get; set; } = DefaultSortDirection;

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        NormalizeState();
        await LoadProductsAsync(useCursor: false, cursorProductId: 0, isNextPage: true);
    }

    public IActionResult OnPostFilter(
        long categoryFilter = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "",
        string orderPreset = "",
        bool clear = false)
    {
        CategoryFilter = clear ? 0 : Math.Max(0, categoryFilter);
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = clear ? string.Empty : NormalizeText(searchTerm);
        ApplyOrderPreset(orderPreset);

        return RedirectToPage(new
        {
            CategoryFilter,
            SortBy,
            SortDirection,
            SearchTerm,
            CurrentPage = 1,
            CurrentAnchorProductId = 0
        });
    }

    public async Task<IActionResult> OnPostNavigateAsync(
        long categoryFilter = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "",
        string navigationMode = "",
        long cursorProductId = 0,
        int currentPage = 1)
    {
        CategoryFilter = Math.Max(0, categoryFilter);
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);
        CurrentPage = Math.Max(1, currentPage);

        var isNextPage = string.Equals(navigationMode, "next", StringComparison.OrdinalIgnoreCase);
        CurrentPage = isNextPage ? CurrentPage + 1 : Math.Max(1, CurrentPage - 1);

        await LoadProductsAsync(useCursor: cursorProductId > 0, cursorProductId, isNextPage);
        return Page();
    }

    public IActionResult OnPostSort(
        string sortBy = "",
        long categoryFilter = 0,
        string currentSortBy = "",
        string currentSortDirection = "",
        string searchTerm = "")
    {
        CategoryFilter = Math.Max(0, categoryFilter);
        SearchTerm = NormalizeText(searchTerm);
        var normalizedCurrentSortBy = NormalizeSortBy(currentSortBy);
        var normalizedCurrentDirection = NormalizeSortDirection(currentSortDirection);
        var normalizedRequestedSortBy = NormalizeSortBy(sortBy);
        var nextDirection = "asc";

        if (string.Equals(normalizedCurrentSortBy, normalizedRequestedSortBy, StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedCurrentDirection, "asc", StringComparison.OrdinalIgnoreCase))
        {
            nextDirection = "desc";
        }

        return RedirectToPage(new
        {
            CategoryFilter,
            SortBy = normalizedRequestedSortBy,
            SortDirection = nextDirection,
            SearchTerm,
            CurrentPage = 1,
            CurrentAnchorProductId = 0
        });
    }

    public string GetSortIcon(string columnName)
    {
        if (!string.Equals(SortBy, NormalizeSortBy(columnName), StringComparison.OrdinalIgnoreCase))
        {
            return "bi-arrow-down-up";
        }

        return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "bi-sort-down"
            : "bi-sort-up";
    }

    private async Task LoadProductsAsync(bool useCursor, long cursorProductId, bool isNextPage)
    {
        var result = await productsApiAdapter.GetProductsAsync(
            CategoryFilter,
            _defaultPageSize,
            SortBy,
            SortDirection,
            useCursor ? 0 : CurrentAnchorProductId,
            useCursor ? cursorProductId : 0,
            isNextPage,
            SearchTerm,
            HttpContext.RequestAborted);

        if (!result.Success || result.Data == null)
        {
            logger.LogWarning(
                "No se pudo cargar el catálogo desde el API: {Errors}",
                string.Join(" | ", result.Errors));

            Products = [];
            Categories = [];
            HasPreviousPage = false;
            HasNextPage = false;
            CurrentAnchorProductId = 0;
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo cargar el catálogo.");
            return;
        }

        Products = result.Data.Products;
        Categories = result.Data.Categories;
        HasPreviousPage = CurrentPage > 1 && result.Data.HasPreviousPage;
        HasNextPage = result.Data.HasNextPage;
        CurrentAnchorProductId = Products.Count > 0 ? Products[0].Id : 0;
        OrderPreset = ResolveOrderPreset(SortBy, SortDirection);

        if (CategoryFilter > 0 && Categories.All(category => category.Id != CategoryFilter))
        {
            CategoryFilter = 0;
        }
    }

    private void NormalizeState()
    {
        CategoryFilter = Math.Max(0, CategoryFilter);
        CurrentPage = Math.Max(1, CurrentPage);
        SortBy = NormalizeSortBy(SortBy);
        SortDirection = NormalizeSortDirection(SortDirection);
        SearchTerm = NormalizeText(SearchTerm);
        OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
    }

    private void ApplyOrderPreset(string orderPreset)
    {
        var normalizedOrderPreset = NormalizeOrderPreset(orderPreset);
        if (string.Equals(normalizedOrderPreset, OrderPresetRecent, StringComparison.Ordinal))
        {
            SortBy = "id";
            SortDirection = "desc";
            OrderPreset = OrderPresetRecent;
            return;
        }

        if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalAsc, StringComparison.Ordinal))
        {
            SortBy = "name";
            SortDirection = "asc";
            OrderPreset = OrderPresetAlphabeticalAsc;
            return;
        }

        if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalDesc, StringComparison.Ordinal))
        {
            SortBy = "name";
            SortDirection = "desc";
            OrderPreset = OrderPresetAlphabeticalDesc;
            return;
        }

        OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
    }

    private static string NormalizeOrderPreset(string orderPreset)
    {
        var normalizedOrderPreset = NormalizeText(orderPreset).ToLowerInvariant();
        return normalizedOrderPreset switch
        {
            OrderPresetRecent => OrderPresetRecent,
            OrderPresetAlphabeticalAsc => OrderPresetAlphabeticalAsc,
            OrderPresetAlphabeticalDesc => OrderPresetAlphabeticalDesc,
            OrderPresetCustom => OrderPresetCustom,
            _ => string.Empty
        };
    }

    private static string ResolveOrderPreset(string sortBy, string sortDirection)
    {
        var normalizedSortBy = NormalizeSortBy(sortBy);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);

        if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal) && string.Equals(normalizedSortDirection, "desc", StringComparison.Ordinal))
        {
            return OrderPresetRecent;
        }

        if (string.Equals(normalizedSortBy, "name", StringComparison.Ordinal) && string.Equals(normalizedSortDirection, "asc", StringComparison.Ordinal))
        {
            return OrderPresetAlphabeticalAsc;
        }

        if (string.Equals(normalizedSortBy, "name", StringComparison.Ordinal) && string.Equals(normalizedSortDirection, "desc", StringComparison.Ordinal))
        {
            return OrderPresetAlphabeticalDesc;
        }

        return OrderPresetCustom;
    }

    private static string NormalizeSortBy(string sortBy)
    {
        var normalizedSortBy = NormalizeText(sortBy).ToLowerInvariant();
        return normalizedSortBy switch
        {
            "id" => "id",
            "stock" => "stock",
            "batch" => "batch",
            "expirationdate" => "expirationdate",
            "price" => "price",
            _ => DefaultSortBy
        };
    }

    private static string NormalizeSortDirection(string sortDirection)
    {
        return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : DefaultSortDirection;
    }

    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static int ResolveDefaultPageSize(IConfiguration configuration)
    {
        return int.TryParse(configuration["Ui:DefaultPageSize"], out var pageSize)
            ? Math.Clamp(pageSize, 5, 50)
            : 10;
    }
}
