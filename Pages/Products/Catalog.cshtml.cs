using System.Globalization;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.categories.application.models;
using Mercadito.src.products.application.models;
using Mercadito.src.products.application.ports.input;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Mercadito.Pages.Products
{
    public class CatalogModel : AppPageModel, IProductListingPageModel
    {
        private const string CurrentPageSessionKey = "Catalog.CurrentPage";
        private const string CategoryFilterSessionKey = "Catalog.CategoryFilter";
        private const string SortBySessionKey = "Catalog.SortBy";
        private const string SortDirectionSessionKey = "Catalog.SortDirection";
        private const string SearchTermSessionKey = "Catalog.SearchTerm";
        private const string CurrentAnchorProductIdSessionKey = "Catalog.CurrentAnchorProductId";
        private const string PendingNavigationModeSessionKey = "Catalog.PendingNavigationMode";
        private const string PendingNavigationCursorProductIdSessionKey = "Catalog.PendingNavigationCursorProductId";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private const string OrderPresetRecent = "recent";
        private const string OrderPresetAlphabeticalAsc = "az";
        private const string OrderPresetAlphabeticalDesc = "za";
        private const string OrderPresetCustom = "custom";
        private const string NavigationModeNext = "next";
        private const string NavigationModePrevious = "prev";

        private readonly ILogger<CatalogModel> _logger;
        private readonly IProductManagementUseCase _productManagementUseCase;
        private readonly int _defaultPageSize;

        public CatalogModel(
            ILogger<CatalogModel> logger,
            IProductManagementUseCase productManagementUseCase,
            IConfiguration configuration)
        {
            _logger = logger;
            _productManagementUseCase = productManagementUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public List<ProductWithCategoriesModel> Products { get; set; } = [];
        public List<CategoryModel> Categories { get; set; } = [];
        public long CategoryFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorProductId { get; set; }
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public string SearchTerm { get; set; } = string.Empty;
        public string OrderPreset { get; set; } = OrderPresetAlphabeticalAsc;

        public async Task OnGetAsync()
        {
            LoadStateFromSession();
            await LoadCategoriesAsync();
            NormalizeCurrentState();

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadProductsByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorProductId);
            }
            else
            {
                await LoadProductsFromAnchorAsync();
            }

            SaveStateInSession();
        }

        public IActionResult OnPostFilter(
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "",
            string orderPreset = "",
            bool clear = false)
        {
            if (clear)
            {
                categoryFilter = 0;
                searchTerm = string.Empty;
            }

            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);
            ApplyOrderPreset(orderPreset);
            CurrentPage = 1;
            CurrentAnchorProductId = 0;
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigate(
            long categoryFilter = 0,
            string sortBy = "",
            string sortDirection = "",
            string searchTerm = "",
            string navigationMode = "",
            long cursorProductId = 0)
        {
            LoadStateFromSession();
            SetFilterAndState(categoryFilter, sortBy, sortDirection, searchTerm);
            SetPendingNavigation(navigationMode, cursorProductId);
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(
            string sortBy = "",
            long categoryFilter = 0,
            string currentSortBy = "",
            string currentSortDirection = "",
            string searchTerm = "")
        {
            SetFilterAndState(categoryFilter, currentSortBy, currentSortDirection, searchTerm);
            ToggleSort(sortBy);
            CurrentPage = 1;
            CurrentAnchorProductId = 0;
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public string GetSortIcon(string columnName)
        {
            var normalizedColumn = NormalizeSortBy(columnName);
            if (!string.Equals(SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return "bi-arrow-down-up";
            }

            return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "bi-sort-down"
                : "bi-sort-up";
        }

        private async Task LoadProductsFromAnchorAsync()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var loadedProducts = (await _productManagementUseCase.GetPageFromAnchorAsync(
                    CategoryFilter,
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    CurrentAnchorProductId,
                    SearchTerm,
                    cancellationToken)).ToList();

                if (loadedProducts.Count == 0 && CurrentAnchorProductId > 0)
                {
                    var previousPageResult = await _productManagementUseCase.GetPageByCursorAsync(
                        CategoryFilter,
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorProductId,
                        isNextPage: false,
                        SearchTerm,
                        cancellationToken);
                    loadedProducts = [.. previousPageResult];

                    if (loadedProducts.Count > 0 && CurrentPage > 1)
                    {
                        CurrentPage--;
                    }
                }

                if (loadedProducts.Count == 0 && CurrentAnchorProductId > 0)
                {
                    CurrentAnchorProductId = 0;
                    CurrentPage = 1;
                    loadedProducts = (await _productManagementUseCase.GetPageFromAnchorAsync(
                        CategoryFilter,
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorProductId,
                        SearchTerm,
                        cancellationToken)).ToList();
                }

                Products = loadedProducts;
                CurrentAnchorProductId = Products.Count > 0 ? Products[0].Id : 0;
                await UpdateNavigationFlagsAsync();
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar catálogo.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar catálogo.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar catálogo");
                Products = [];
                HasPreviousPage = false;
                HasNextPage = false;
                CurrentAnchorProductId = 0;
                CurrentPage = 1;
            }
        }

        private async Task LoadProductsByCursorAsync(bool isNextPage, long cursorProductId)
        {
            if (cursorProductId <= 0)
            {
                await LoadProductsFromAnchorAsync();
                return;
            }

            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var result = await _productManagementUseCase.GetPageByCursorAsync(
                    CategoryFilter,
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    cursorProductId,
                    isNextPage,
                    SearchTerm,
                    cancellationToken);

                if (result.Count == 0)
                {
                    await LoadProductsFromAnchorAsync();
                    return;
                }

                Products = [.. result];
                CurrentAnchorProductId = Products.Count > 0 ? Products[0].Id : 0;
                if (isNextPage)
                {
                    CurrentPage++;
                }
                else if (CurrentPage > 1)
                {
                    CurrentPage--;
                }

                await UpdateNavigationFlagsAsync();
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar catálogo con cursor.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar catálogo con cursor.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar catálogo con cursor");
                await LoadProductsFromAnchorAsync();
            }
        }

        private async Task UpdateNavigationFlagsAsync()
        {
            if (Products.Count == 0)
            {
                HasPreviousPage = false;
                HasNextPage = false;
                return;
            }

            var firstProductId = Products[0].Id;
            var lastProductId = Products[Products.Count - 1].Id;
            var cancellationToken = HttpContext.RequestAborted;

            HasPreviousPage = await _productManagementUseCase.HasProductsByCursorAsync(
                CategoryFilter,
                SortBy,
                SortDirection,
                firstProductId,
                isNextPage: false,
                SearchTerm,
                cancellationToken);

            HasNextPage = await _productManagementUseCase.HasProductsByCursorAsync(
                CategoryFilter,
                SortBy,
                SortDirection,
                lastProductId,
                isNextPage: true,
                SearchTerm,
                cancellationToken);

            if (CurrentPage <= 1)
            {
                HasPreviousPage = false;
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                Categories = [.. await _productManagementUseCase.GetCategoriesAsync(HttpContext.RequestAborted)];
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para catálogo.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para catálogo.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías del catálogo");
                Categories = [];
            }
        }

        private void SetFilterAndState(long categoryFilter, string sortBy, string sortDirection, string searchTerm)
        {
            CategoryFilter = categoryFilter >= 0 ? categoryFilter : 0;
            SearchTerm = ResolveSearchTermFromRequest(searchTerm);

            if (string.IsNullOrWhiteSpace(sortBy) && string.IsNullOrWhiteSpace(sortDirection))
            {
                LoadSortStateFromSession();
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            SortBy = NormalizeSortBy(sortBy);
            SortDirection = NormalizeSortDirection(sortDirection);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void NormalizeCurrentState()
        {
            CurrentPage = CurrentPage > 0 ? CurrentPage : 1;
            CategoryFilter = CategoryFilter >= 0 ? CategoryFilter : 0;
            CurrentAnchorProductId = CurrentAnchorProductId >= 0 ? CurrentAnchorProductId : 0;
            if (CurrentAnchorProductId == 0)
            {
                CurrentPage = 1;
            }

            SearchTerm = NormalizeSearchTerm(SearchTerm);
            SortBy = NormalizeSortBy(SortBy);
            SortDirection = NormalizeSortDirection(SortDirection);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);

            if (CategoryFilter > 0 && Categories.Count > 0 && !Categories.Exists(category => category.Id == CategoryFilter))
            {
                CategoryFilter = 0;
            }
        }

        private void LoadStateFromSession()
        {
            var currentPageInSession = HttpContext.Session.GetInt32(CurrentPageSessionKey);
            CurrentPage = !currentPageInSession.HasValue || currentPageInSession.Value <= 0
                ? 1
                : currentPageInSession.Value;

            var rawCategoryFilter = HttpContext.Session.GetString(CategoryFilterSessionKey);
            CategoryFilter = long.TryParse(rawCategoryFilter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCategoryFilter) && parsedCategoryFilter >= 0
                ? parsedCategoryFilter
                : 0;

            var persistedSearchTerm = HttpContext.Session.GetString(SearchTermSessionKey);
            SearchTerm = NormalizeSearchTerm(persistedSearchTerm ?? string.Empty);

            var rawAnchorProductId = HttpContext.Session.GetString(CurrentAnchorProductIdSessionKey);
            CurrentAnchorProductId = long.TryParse(rawAnchorProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAnchorProductId) && parsedAnchorProductId >= 0
                ? parsedAnchorProductId
                : 0;

            LoadSortStateFromSession();
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
            HttpContext.Session.SetString(CategoryFilterSessionKey, Math.Max(CategoryFilter, 0).ToString(CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(SearchTermSessionKey, NormalizeSearchTerm(SearchTerm));
            HttpContext.Session.SetString(SortBySessionKey, NormalizeSortBy(SortBy));
            HttpContext.Session.SetString(SortDirectionSessionKey, NormalizeSortDirection(SortDirection));
            HttpContext.Session.SetString(CurrentAnchorProductIdSessionKey, Math.Max(CurrentAnchorProductId, 0).ToString(CultureInfo.InvariantCulture));
        }

        private void LoadSortStateFromSession()
        {
            SortBy = NormalizeSortBy(HttpContext.Session.GetString(SortBySessionKey) ?? string.Empty);
            SortDirection = NormalizeSortDirection(HttpContext.Session.GetString(SortDirectionSessionKey) ?? string.Empty);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void ToggleSort(string sortBy)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            if (string.Equals(SortBy, normalizedSortBy, StringComparison.OrdinalIgnoreCase))
            {
                SortDirection = string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            SortBy = normalizedSortBy;
            SortDirection = DefaultSortDirection;
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void ApplyOrderPreset(string orderPreset)
        {
            var normalizedOrderPreset = NormalizeOrderPreset(orderPreset);
            if (string.IsNullOrWhiteSpace(normalizedOrderPreset) || string.Equals(normalizedOrderPreset, OrderPresetCustom, StringComparison.Ordinal))
            {
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetRecent, StringComparison.Ordinal))
            {
                SortBy = "id";
                SortDirection = "desc";
                OrderPreset = OrderPresetRecent;
                CurrentPage = 1;
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalAsc, StringComparison.Ordinal))
            {
                SortBy = "name";
                SortDirection = "asc";
                OrderPreset = OrderPresetAlphabeticalAsc;
                CurrentPage = 1;
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalDesc, StringComparison.Ordinal))
            {
                SortBy = "name";
                SortDirection = "desc";
                OrderPreset = OrderPresetAlphabeticalDesc;
                CurrentPage = 1;
                return;
            }

            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void SetPendingNavigation(string navigationMode, long cursorProductId)
        {
            if (cursorProductId <= 0 || !TryResolveNavigationMode(navigationMode, out var normalizedNavigationMode))
            {
                ClearPendingNavigation();
                return;
            }

            HttpContext.Session.SetString(PendingNavigationModeSessionKey, normalizedNavigationMode);
            HttpContext.Session.SetString(PendingNavigationCursorProductIdSessionKey, cursorProductId.ToString(CultureInfo.InvariantCulture));
        }

        private PendingNavigationState? PopPendingNavigation()
        {
            var rawNavigationMode = HttpContext.Session.GetString(PendingNavigationModeSessionKey);
            var rawCursorProductId = HttpContext.Session.GetString(PendingNavigationCursorProductIdSessionKey);
            ClearPendingNavigation();

            if (!TryResolveNavigationMode(rawNavigationMode, out var normalizedNavigationMode))
            {
                return null;
            }

            if (!long.TryParse(rawCursorProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cursorProductId) || cursorProductId <= 0)
            {
                return null;
            }

            return new PendingNavigationState(
                string.Equals(normalizedNavigationMode, NavigationModeNext, StringComparison.Ordinal),
                cursorProductId);
        }

        private void ClearPendingNavigation()
        {
            HttpContext.Session.Remove(PendingNavigationModeSessionKey);
            HttpContext.Session.Remove(PendingNavigationCursorProductIdSessionKey);
        }

        private static bool TryResolveNavigationMode(string? navigationMode, out string normalizedNavigationMode)
        {
            if (string.Equals(navigationMode, NavigationModeNext, StringComparison.OrdinalIgnoreCase))
            {
                normalizedNavigationMode = NavigationModeNext;
                return true;
            }

            if (string.Equals(navigationMode, NavigationModePrevious, StringComparison.OrdinalIgnoreCase))
            {
                normalizedNavigationMode = NavigationModePrevious;
                return true;
            }

            normalizedNavigationMode = string.Empty;
            return false;
        }

        private string ResolveSearchTermFromRequest(string searchTerm)
        {
            var hasSearchTermInForm = Request.HasFormContentType && Request.Form.ContainsKey("searchTerm");
            var hasSearchTermInQuery = Request.Query.ContainsKey("searchTerm");

            if (hasSearchTermInForm || hasSearchTermInQuery)
            {
                return NormalizeSearchTerm(searchTerm);
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var persistedSearchTerm = HttpContext.Session.GetString(SearchTermSessionKey);
                return NormalizeSearchTerm(persistedSearchTerm ?? string.Empty);
            }

            return NormalizeSearchTerm(searchTerm);
        }

        private static string NormalizeOrderPreset(string orderPreset)
        {
            if (string.IsNullOrWhiteSpace(orderPreset))
            {
                return string.Empty;
            }

            var normalizedOrderPreset = orderPreset.Trim().ToLowerInvariant();
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
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return DefaultSortBy;
            }

            var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "id" => "id",
                "stock" => "stock",
                "batch" => "batch",
                "expirationdate" => "expirationdate",
                "price" => "price",
                _ => "name"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";
        }

        private static string NormalizeSearchTerm(string searchTerm)
        {
            return string.IsNullOrWhiteSpace(searchTerm)
                ? string.Empty
                : searchTerm.Trim();
        }

        private readonly struct PendingNavigationState(bool isNextPage, long cursorProductId)
        {
            public bool IsNextPage { get; } = isNextPage;
            public long CursorProductId { get; } = cursorProductId;
        }
    }
}
