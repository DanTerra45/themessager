using Mercadito.Frontend.Adapters.Products;
using Mercadito.Frontend.Dtos.Categories;
using Mercadito.Frontend.Dtos.Products;
using Mercadito.Frontend.Pages.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Frontend.Pages.Products;

public sealed class ProductsModel(
    IProductsApiAdapter productsApiAdapter,
    IConfiguration configuration,
    ILogger<ProductsModel> logger) : FrontendPageModel, IProductListingPageModel
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
    public bool ShowModal { get; private set; }
    public bool ShowEditModal { get; private set; }

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

    [BindProperty]
    public ProductFormDto NewProduct { get; set; } = CreateDefaultProductForm();

    [BindProperty]
    public ProductFormDto EditProduct { get; set; } = CreateDefaultProductForm();

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

    public async Task<IActionResult> OnPostStartEditAsync(
        long id,
        long categoryFilter = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        CategoryFilter = Math.Max(0, categoryFilter);
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);

        var result = await productsApiAdapter.GetProductAsync(id, HttpContext.RequestAborted);
        if (result.Success && result.Data != null)
        {
            EditProduct = ToForm(result.Data);
            ShowEditModal = true;
        }
        else
        {
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudo cargar el producto.");
        }

        await LoadProductsAsync(useCursor: false, cursorProductId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(
        long categoryFilter = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        CategoryFilter = Math.Max(0, categoryFilter);
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);
        EnsureProductDefaults(NewProduct);
        RemoveModelStateForPrefix(nameof(EditProduct));

        if (!IsModelStateValidForPrefix(nameof(NewProduct)))
        {
            LogInvalidModelState(logger, "Products.Create");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario.";
            ShowModal = true;
            await LoadProductsAsync(useCursor: false, cursorProductId: 0, isNextPage: true);
            return Page();
        }

        var result = await productsApiAdapter.CreateProductAsync(
            ToSaveRequest(NewProduct),
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Producto agregado exitosamente.";
            return RedirectToCurrentState(resetToFirstPage: IsRecentOrderPreset(OrderPreset));
        }

        ApplyApiErrors(result, nameof(NewProduct));
        TempData["ErrorMessage"] = FirstErrorOrDefault(result, "Corrige los errores del formulario.");
        ShowModal = true;
        await LoadProductsAsync(useCursor: false, cursorProductId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(
        long categoryFilter = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        CategoryFilter = Math.Max(0, categoryFilter);
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);
        EnsureProductDefaults(EditProduct);
        RemoveModelStateForPrefix(nameof(NewProduct));

        if (!IsModelStateValidForPrefix(nameof(EditProduct)))
        {
            LogInvalidModelState(logger, "Products.Edit");
            TempData["ErrorMessage"] = "Revisa los campos obligatorios del formulario de edición.";
            ShowEditModal = true;
            await LoadProductsAsync(useCursor: false, cursorProductId: 0, isNextPage: true);
            return Page();
        }

        var result = await productsApiAdapter.UpdateProductAsync(
            EditProduct.Id,
            ToSaveRequest(EditProduct),
            BuildActorContext(),
            HttpContext.RequestAborted);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Producto actualizado correctamente.";
            return RedirectToCurrentState(resetToFirstPage: false);
        }

        ApplyApiErrors(result, nameof(EditProduct));
        TempData["ErrorMessage"] = FirstErrorOrDefault(result, "Corrige los errores del formulario.");
        ShowEditModal = true;
        await LoadProductsAsync(useCursor: false, cursorProductId: 0, isNextPage: true);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        long id,
        long categoryFilter = 0,
        string sortBy = "",
        string sortDirection = "",
        string searchTerm = "")
    {
        CategoryFilter = Math.Max(0, categoryFilter);
        SortBy = NormalizeSortBy(sortBy);
        SortDirection = NormalizeSortDirection(sortDirection);
        SearchTerm = NormalizeText(searchTerm);

        var result = await productsApiAdapter.DeleteProductAsync(
            id,
            BuildActorContext(),
            HttpContext.RequestAborted);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Producto desactivado."
            : FirstErrorOrDefault(result, "No se pudo eliminar el producto.");

        return RedirectToCurrentState(resetToFirstPage: false);
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
                "No se pudieron cargar productos desde el API: {Errors}",
                string.Join(" | ", result.Errors));

            Products = [];
            Categories = [];
            HasPreviousPage = false;
            HasNextPage = false;
            CurrentAnchorProductId = 0;
            TempData["ErrorMessage"] = FirstErrorOrDefault(result, "No se pudieron cargar los productos.");
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

    private RedirectToPageResult RedirectToCurrentState(bool resetToFirstPage)
    {
        return RedirectToPage(new
        {
            CategoryFilter,
            SortBy,
            SortDirection,
            SearchTerm,
            CurrentPage = resetToFirstPage ? 1 : CurrentPage,
            CurrentAnchorProductId = resetToFirstPage ? 0 : CurrentAnchorProductId
        });
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

    private static bool IsRecentOrderPreset(string orderPreset)
    {
        return string.Equals(orderPreset, OrderPresetRecent, StringComparison.Ordinal);
    }

    private static ProductFormDto ToForm(ProductForEditDto product)
    {
        return new ProductFormDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Stock = product.Stock,
            Batch = product.Batch,
            ExpirationDate = product.ExpirationDate,
            Price = product.Price,
            CategoryIds = product.CategoryIds.ToList()
        };
    }

    private static SaveProductRequestDto ToSaveRequest(ProductFormDto product)
    {
        return new SaveProductRequestDto(
            NormalizeCollapsed(product.Name),
            NormalizeText(product.Description),
            product.Stock,
            NormalizeText(product.Batch),
            product.ExpirationDate,
            product.Price,
            product.CategoryIds.Where(categoryId => categoryId > 0).Distinct().ToList());
    }

    private static void EnsureProductDefaults(ProductFormDto product)
    {
        if (product.ExpirationDate == default)
        {
            product.ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
        }

        if (!product.Stock.HasValue || product.Stock.Value < 0)
        {
            product.Stock = 0;
        }

        if (!product.Price.HasValue || product.Price.Value < 0.01m)
        {
            product.Price = 0.01m;
        }
    }

    private static ProductFormDto CreateDefaultProductForm()
    {
        return new ProductFormDto
        {
            Name = string.Empty,
            Description = string.Empty,
            Stock = 0,
            Batch = string.Empty,
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
            Price = 0.01m
        };
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

    private static string NormalizeCollapsed(string? value)
    {
        return string.Join(' ', NormalizeText(value).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int ResolveDefaultPageSize(IConfiguration configuration)
    {
        return int.TryParse(configuration["Ui:DefaultPageSize"], out var pageSize)
            ? Math.Clamp(pageSize, 5, 50)
            : 10;
    }
}
