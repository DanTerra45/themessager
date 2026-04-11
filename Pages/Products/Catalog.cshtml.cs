using Mercadito.Pages.Infrastructure;
using Mercadito.src.categories.application.models;
using Mercadito.src.products.application.models;
using Mercadito.src.products.application.ports.input;
using Microsoft.AspNetCore.Mvc;
using Mercadito.src.shared.domain.exceptions;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.Pages.Products
{
    public class CatalogModel(
        ILogger<CatalogModel> logger,
        IListingPageStateService listingPageStateService,
        IProductManagementUseCase productManagementUseCase,
        IConfiguration configuration) : AppPageModel, IProductListingPageModel
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
        private static readonly KeysetListingSessionKeys ListingSessionKeys = new(
            CurrentPageSessionKey,
            CurrentAnchorProductIdSessionKey,
            PendingNavigationModeSessionKey,
            PendingNavigationCursorProductIdSessionKey,
            SortBySessionKey,
            SortDirectionSessionKey,
            SearchTermSessionKey);
        private static readonly ListingPageStateOptions ListingStateOptions = new(
            ListingSessionKeys,
            DefaultSortBy,
            DefaultSortDirection,
            NormalizeSortBy,
            NormalizeSortDirection,
            ValidationText.NormalizeTrimmed);

        private readonly ILogger<CatalogModel> _logger = logger;
        private readonly IListingPageStateService _listingPageStateService = listingPageStateService;
        private readonly IProductManagementUseCase _productManagementUseCase = productManagementUseCase;
        private readonly int _defaultPageSize = PaginationSettings.ResolveDefaultPageSize(configuration);

        public IReadOnlyList<ProductWithCategoriesModel> Products { get; set; } = [];
        public IReadOnlyList<CategoryModel> Categories { get; set; } = [];
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
                await LoadProductsByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorId);
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
            return _listingPageStateService.GetSortIcon(SortBy, SortDirection, columnName, ListingStateOptions);
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
                    loadedProducts = [.. await _productManagementUseCase.GetPageFromAnchorAsync(
                        CategoryFilter,
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorProductId,
                        SearchTerm,
                        cancellationToken)];
                }

                Products = loadedProducts;
                CurrentAnchorProductId = _listingPageStateService.ResolveCurrentAnchorId(Products, product => product.Id);
                await UpdateNavigationFlagsAsync();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar catálogo.");
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
                CurrentAnchorProductId = _listingPageStateService.ResolveCurrentAnchorId(Products, product => product.Id);
                CurrentPage = _listingPageStateService.MoveCurrentPage(CurrentPage, isNextPage);

                await UpdateNavigationFlagsAsync();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar catálogo con cursor.");
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
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para catálogo.");
                Categories = [];
            }
        }

        private void SetFilterAndState(long categoryFilter, string sortBy, string sortDirection, string searchTerm)
        {
            CategoryFilter = 0;
            if (categoryFilter >= 0)
            {
                CategoryFilter = categoryFilter;
            }
            SearchTerm = ResolveSearchTermFromRequest(searchTerm);
            (SortBy, SortDirection) = _listingPageStateService.ResolveSortState(HttpContext.Session, ListingStateOptions, sortBy, sortDirection);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void NormalizeCurrentState()
        {
            if (CategoryFilter < 0)
            {
                CategoryFilter = 0;
            }
            ListingState = _listingPageStateService.NormalizeState(ListingState, ListingStateOptions);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);

            if (CategoryFilter > 0 && Categories.Count > 0 && !Categories.Any(category => category.Id == CategoryFilter))
            {
                CategoryFilter = 0;
            }
        }

        private void LoadStateFromSession()
        {
            ListingState = _listingPageStateService.LoadState(HttpContext.Session, ListingStateOptions);
            CategoryFilter = _listingPageStateService.LoadNonNegativeLong(HttpContext.Session, CategoryFilterSessionKey);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void SaveStateInSession()
        {
            _listingPageStateService.SaveState(HttpContext.Session, ListingState, ListingStateOptions);
            _listingPageStateService.SaveNonNegativeLong(HttpContext.Session, CategoryFilterSessionKey, CategoryFilter);
        }

        private void ToggleSort(string sortBy)
        {
            (SortBy, SortDirection) = _listingPageStateService.ToggleSort(SortBy, SortDirection, sortBy, ListingStateOptions);
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
            _listingPageStateService.SetPendingNavigation(HttpContext.Session, ListingSessionKeys, navigationMode, cursorProductId);
        }

        private KeysetPendingNavigationState? PopPendingNavigation()
        {
            return _listingPageStateService.PopPendingNavigation(HttpContext.Session, ListingSessionKeys);
        }

        private void ClearPendingNavigation()
        {
            _listingPageStateService.ClearPendingNavigation(HttpContext.Session, ListingSessionKeys);
        }

        private string ResolveSearchTermFromRequest(string searchTerm)
        {
            return _listingPageStateService.ResolveSearchTermFromRequest(Request, HttpContext.Session, ListingStateOptions, searchTerm);
        }

        private KeysetListingSessionState ListingState
        {
            get
            {
                return new KeysetListingSessionState(CurrentPage, CurrentAnchorProductId, SortBy, SortDirection, SearchTerm);
            }
            set
            {
                CurrentPage = value.CurrentPage;
                CurrentAnchorProductId = value.CurrentAnchorId;
                SearchTerm = value.SearchTerm;
                SortBy = value.SortBy;
                SortDirection = value.SortDirection;
            }
        }

        private static string NormalizeOrderPreset(string orderPreset)
        {
            if (string.IsNullOrWhiteSpace(orderPreset))
            {
                return string.Empty;
            }

            var normalizedOrderPreset = ValidationText.NormalizeLowerTrimmed(orderPreset);
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

            var normalizedSortBy = ValidationText.NormalizeLowerTrimmed(sortBy);
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
            if (string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                return "desc";
            }

            return "asc";
        }
    }
}
