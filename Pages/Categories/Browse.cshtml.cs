using Mercadito.Pages.Infrastructure;
using Mercadito.src.categories.application.models;
using Mercadito.src.categories.application.ports.input;
using Microsoft.AspNetCore.Mvc;
using Mercadito.src.shared.domain.exceptions;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.Pages.Categories
{
    public class BrowseModel(
        ILogger<BrowseModel> logger,
        IListingPageStateService listingPageStateService,
        ICategoryManagementUseCase categoryManagementUseCase,
        IConfiguration configuration) : AppPageModel
    {
        private const string CurrentPageSessionKey = "CategoriesBrowse.CurrentPage";
        private const string CurrentAnchorCategoryIdSessionKey = "CategoriesBrowse.CurrentAnchorCategoryId";
        private const string PendingNavigationModeSessionKey = "CategoriesBrowse.PendingNavigationMode";
        private const string PendingNavigationCursorCategoryIdSessionKey = "CategoriesBrowse.PendingNavigationCursorCategoryId";
        private const string SortBySessionKey = "CategoriesBrowse.SortBy";
        private const string SortDirectionSessionKey = "CategoriesBrowse.SortDirection";
        private const string SearchTermSessionKey = "CategoriesBrowse.SearchTerm";
        private const string DefaultSortBy = "name";
        private const string DefaultSortDirection = "asc";
        private static readonly KeysetListingSessionKeys ListingSessionKeys = new(
            CurrentPageSessionKey,
            CurrentAnchorCategoryIdSessionKey,
            PendingNavigationModeSessionKey,
            PendingNavigationCursorCategoryIdSessionKey,
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

        private readonly ILogger<BrowseModel> _logger = logger;
        private readonly IListingPageStateService _listingPageStateService = listingPageStateService;
        private readonly ICategoryManagementUseCase _categoryManagementUseCase = categoryManagementUseCase;
        private readonly int _defaultPageSize = PaginationSettings.ResolveDefaultPageSize(configuration);

        public IReadOnlyList<CategoryModel> Categories { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public long CurrentAnchorCategoryId { get; set; }
        public string SortBy { get; set; } = DefaultSortBy;
        public string SortDirection { get; set; } = DefaultSortDirection;
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            LoadStateFromSession();
            NormalizeCurrentState();

            var pendingNavigation = PopPendingNavigation();
            if (pendingNavigation.HasValue)
            {
                await LoadCategoriesByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorId);
            }
            else
            {
                await LoadCategoriesFromAnchorAsync();
            }

            SaveStateInSession();
        }

        public IActionResult OnPostFilter(string sortBy = "", string sortDirection = "", string searchTerm = "", bool clear = false)
        {
            LoadStateFromSession();
            var effectiveSearchTerm = searchTerm;
            if (clear)
            {
                effectiveSearchTerm = string.Empty;
            }

            SetSearchAndSortState(effectiveSearchTerm, sortBy, sortDirection);
            CurrentPage = 1;
            CurrentAnchorCategoryId = 0;
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostNavigate(string navigationMode = "", long cursorCategoryId = 0, string sortBy = "", string sortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, sortBy, sortDirection);
            SetPendingNavigation(navigationMode, cursorCategoryId);
            SaveStateInSession();
            return RedirectToPage();
        }

        public IActionResult OnPostSort(string sortBy = "", string currentSortBy = "", string currentSortDirection = "")
        {
            LoadStateFromSession();
            SetSearchAndSortState(string.Empty, currentSortBy, currentSortDirection);
            ToggleSort(sortBy);
            CurrentPage = 1;
            CurrentAnchorCategoryId = 0;
            ClearPendingNavigation();
            SaveStateInSession();
            return RedirectToPage();
        }

        public string GetSortIcon(string columnName)
        {
            return _listingPageStateService.GetSortIcon(SortBy, SortDirection, columnName, ListingStateOptions);
        }

        private void SetSearchAndSortState(string searchTerm, string sortBy, string sortDirection)
        {
            SearchTerm = ResolveSearchTermFromRequest(searchTerm);
            (SortBy, SortDirection) = _listingPageStateService.ResolveSortState(HttpContext.Session, ListingStateOptions, sortBy, sortDirection);
        }

        private void LoadStateFromSession()
        {
            ListingState = _listingPageStateService.LoadState(HttpContext.Session, ListingStateOptions);
        }

        private void SaveStateInSession()
        {
            _listingPageStateService.SaveState(HttpContext.Session, ListingState, ListingStateOptions);
        }

        private void NormalizeCurrentState()
        {
            ListingState = _listingPageStateService.NormalizeState(ListingState, ListingStateOptions);
        }

        private void ToggleSort(string sortBy)
        {
            (SortBy, SortDirection) = _listingPageStateService.ToggleSort(SortBy, SortDirection, sortBy, ListingStateOptions);
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
                "code" => "code",
                "productcount" => "productcount",
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

        private string ResolveSearchTermFromRequest(string searchTerm)
        {
            return _listingPageStateService.ResolveSearchTermFromRequest(Request, HttpContext.Session, ListingStateOptions, searchTerm);
        }

        private KeysetListingSessionState ListingState
        {
            get
            {
                return new KeysetListingSessionState(CurrentPage, CurrentAnchorCategoryId, SortBy, SortDirection, SearchTerm);
            }
            set
            {
                CurrentPage = value.CurrentPage;
                CurrentAnchorCategoryId = value.CurrentAnchorId;
                SortBy = value.SortBy;
                SortDirection = value.SortDirection;
                SearchTerm = value.SearchTerm;
            }
        }

        private async Task LoadCategoriesFromAnchorAsync()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var loadedCategories = (await _categoryManagementUseCase.GetPageFromAnchorAsync(
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    CurrentAnchorCategoryId,
                    SearchTerm,
                    cancellationToken)).ToList();

                if (loadedCategories.Count == 0 && CurrentAnchorCategoryId > 0)
                {
                    loadedCategories = [.. await _categoryManagementUseCase.GetPageByCursorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorCategoryId,
                        isNextPage: false,
                        SearchTerm,
                        cancellationToken)];

                    if (loadedCategories.Count > 0 && CurrentPage > 1)
                    {
                        CurrentPage--;
                    }
                }

                if (loadedCategories.Count == 0 && CurrentAnchorCategoryId > 0)
                {
                    CurrentAnchorCategoryId = 0;
                    CurrentPage = 1;
                    loadedCategories = [.. await _categoryManagementUseCase.GetPageFromAnchorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorCategoryId,
                        SearchTerm,
                        cancellationToken)];
                }

                Categories = loadedCategories;
                CurrentAnchorCategoryId = _listingPageStateService.ResolveCurrentAnchorId(Categories, category => category.Id);
                await UpdateNavigationFlagsAsync();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar consulta de categorías.");
                Categories = [];
                HasPreviousPage = false;
                HasNextPage = false;
                CurrentAnchorCategoryId = 0;
                CurrentPage = 1;
            }
        }

        private async Task LoadCategoriesByCursorAsync(bool isNextPage, long cursorCategoryId)
        {
            if (cursorCategoryId <= 0)
            {
                await LoadCategoriesFromAnchorAsync();
                return;
            }

            try
            {
                var categories = await _categoryManagementUseCase.GetPageByCursorAsync(
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    cursorCategoryId,
                    isNextPage,
                    SearchTerm,
                    HttpContext.RequestAborted);

                if (categories.Count == 0)
                {
                    await LoadCategoriesFromAnchorAsync();
                    return;
                }

                Categories = [.. categories];
                CurrentAnchorCategoryId = _listingPageStateService.ResolveCurrentAnchorId(Categories, category => category.Id);
                CurrentPage = _listingPageStateService.MoveCurrentPage(CurrentPage, isNextPage);

                await UpdateNavigationFlagsAsync();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar consulta de categorías con cursor.");
                await LoadCategoriesFromAnchorAsync();
            }
        }

        private async Task UpdateNavigationFlagsAsync()
        {
            if (Categories.Count == 0)
            {
                HasPreviousPage = false;
                HasNextPage = false;
                return;
            }

            var firstCategoryId = Categories[0].Id;
            var lastCategoryId = Categories[Categories.Count - 1].Id;

            HasPreviousPage = await _categoryManagementUseCase.HasCategoriesByCursorAsync(
                SortBy,
                SortDirection,
                firstCategoryId,
                isNextPage: false,
                SearchTerm,
                HttpContext.RequestAborted);

            HasNextPage = await _categoryManagementUseCase.HasCategoriesByCursorAsync(
                SortBy,
                SortDirection,
                lastCategoryId,
                isNextPage: true,
                SearchTerm,
                HttpContext.RequestAborted);

            if (CurrentPage <= 1)
            {
                HasPreviousPage = false;
            }
        }

        private void SetPendingNavigation(string navigationMode, long cursorCategoryId)
        {
            _listingPageStateService.SetPendingNavigation(HttpContext.Session, ListingSessionKeys, navigationMode, cursorCategoryId);
        }

        private KeysetPendingNavigationState? PopPendingNavigation()
        {
            return _listingPageStateService.PopPendingNavigation(HttpContext.Session, ListingSessionKeys);
        }

        private void ClearPendingNavigation()
        {
            _listingPageStateService.ClearPendingNavigation(HttpContext.Session, ListingSessionKeys);
        }

    }
}
