using System.Globalization;
using Mercadito.Pages.Infrastructure;
using Mercadito.src.categories.application.models;
using Mercadito.src.categories.application.ports.input;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Mercadito.Pages.Categories
{
    public class BrowseModel : AppPageModel
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
        private const string NavigationModeNext = "next";
        private const string NavigationModePrevious = "prev";

        private readonly ILogger<BrowseModel> _logger;
        private readonly ICategoryManagementUseCase _categoryManagementUseCase;
        private readonly int _defaultPageSize;

        public BrowseModel(
            ILogger<BrowseModel> logger,
            ICategoryManagementUseCase categoryManagementUseCase,
            IConfiguration configuration)
        {
            _logger = logger;
            _categoryManagementUseCase = categoryManagementUseCase;
            var configuredPageSize = configuration.GetValue<int>("Pagination:DefaultPageSize");
            _defaultPageSize = configuredPageSize > 0 ? configuredPageSize : 10;
        }

        public List<CategoryModel> Categories { get; set; } = [];
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
                await LoadCategoriesByCursorAsync(pendingNavigation.Value.IsNextPage, pendingNavigation.Value.CursorCategoryId);
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
            SetSearchAndSortState(clear ? string.Empty : searchTerm, sortBy, sortDirection);
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
            var normalizedColumn = NormalizeSortBy(columnName);
            if (!string.Equals(SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return "bi-arrow-down-up";
            }

            return string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "bi-sort-down"
                : "bi-sort-up";
        }

        private void SetSearchAndSortState(string searchTerm, string sortBy, string sortDirection)
        {
            SearchTerm = ResolveSearchTermFromRequest(searchTerm);

            if (string.IsNullOrWhiteSpace(sortBy) && string.IsNullOrWhiteSpace(sortDirection))
            {
                LoadSortStateFromSession();
                return;
            }

            SortBy = NormalizeSortBy(sortBy);
            SortDirection = NormalizeSortDirection(sortDirection);
        }

        private void LoadStateFromSession()
        {
            var currentPageInSession = HttpContext.Session.GetInt32(CurrentPageSessionKey);
            CurrentPage = !currentPageInSession.HasValue || currentPageInSession.Value <= 0
                ? 1
                : currentPageInSession.Value;

            var rawAnchorCategoryId = HttpContext.Session.GetString(CurrentAnchorCategoryIdSessionKey);
            CurrentAnchorCategoryId = long.TryParse(rawAnchorCategoryId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAnchorCategoryId) && parsedAnchorCategoryId >= 0
                ? parsedAnchorCategoryId
                : 0;

            LoadSortStateFromSession();
            SearchTerm = NormalizeSearchTerm(HttpContext.Session.GetString(SearchTermSessionKey) ?? string.Empty);
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
            HttpContext.Session.SetString(CurrentAnchorCategoryIdSessionKey, Math.Max(CurrentAnchorCategoryId, 0).ToString(CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(SortBySessionKey, NormalizeSortBy(SortBy));
            HttpContext.Session.SetString(SortDirectionSessionKey, NormalizeSortDirection(SortDirection));
            HttpContext.Session.SetString(SearchTermSessionKey, NormalizeSearchTerm(SearchTerm));
        }

        private void NormalizeCurrentState()
        {
            CurrentPage = CurrentPage > 0 ? CurrentPage : 1;
            CurrentAnchorCategoryId = CurrentAnchorCategoryId >= 0 ? CurrentAnchorCategoryId : 0;
            if (CurrentAnchorCategoryId == 0)
            {
                CurrentPage = 1;
            }

            SortBy = NormalizeSortBy(SortBy);
            SortDirection = NormalizeSortDirection(SortDirection);
            SearchTerm = NormalizeSearchTerm(SearchTerm);
        }

        private void LoadSortStateFromSession()
        {
            SortBy = NormalizeSortBy(HttpContext.Session.GetString(SortBySessionKey) ?? string.Empty);
            SortDirection = NormalizeSortDirection(HttpContext.Session.GetString(SortDirectionSessionKey) ?? string.Empty);
        }

        private void ToggleSort(string sortBy)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            if (string.Equals(SortBy, normalizedSortBy, StringComparison.OrdinalIgnoreCase))
            {
                SortDirection = string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                return;
            }

            SortBy = normalizedSortBy;
            SortDirection = DefaultSortDirection;
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
                "code" => "code",
                "productcount" => "productcount",
                _ => "name"
            };
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            return string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";
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
                return NormalizeSearchTerm(HttpContext.Session.GetString(SearchTermSessionKey) ?? string.Empty);
            }

            return NormalizeSearchTerm(searchTerm);
        }

        private static string NormalizeSearchTerm(string searchTerm)
        {
            return string.IsNullOrWhiteSpace(searchTerm) ? string.Empty : searchTerm.Trim();
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
                    loadedCategories = (await _categoryManagementUseCase.GetPageByCursorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorCategoryId,
                        isNextPage: false,
                        SearchTerm,
                        cancellationToken)).ToList();

                    if (loadedCategories.Count > 0 && CurrentPage > 1)
                    {
                        CurrentPage--;
                    }
                }

                if (loadedCategories.Count == 0 && CurrentAnchorCategoryId > 0)
                {
                    CurrentAnchorCategoryId = 0;
                    CurrentPage = 1;
                    loadedCategories = (await _categoryManagementUseCase.GetPageFromAnchorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorCategoryId,
                        SearchTerm,
                        cancellationToken)).ToList();
                }

                Categories = loadedCategories;
                CurrentAnchorCategoryId = Categories.Count > 0 ? Categories[0].Id : 0;
                await UpdateNavigationFlagsAsync();
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar consulta de categorías.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar consulta de categorías.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar consulta de categorías");
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
                CurrentAnchorCategoryId = Categories.Count > 0 ? Categories[0].Id : 0;
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
                _logger.LogError(exception, "Base de datos no disponible al cargar consulta de categorías con cursor.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar consulta de categorías con cursor.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar consulta de categorías con cursor");
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
            if (cursorCategoryId <= 0 || !TryResolveNavigationMode(navigationMode, out var normalizedNavigationMode))
            {
                ClearPendingNavigation();
                return;
            }

            HttpContext.Session.SetString(PendingNavigationModeSessionKey, normalizedNavigationMode);
            HttpContext.Session.SetString(PendingNavigationCursorCategoryIdSessionKey, cursorCategoryId.ToString(CultureInfo.InvariantCulture));
        }

        private PendingNavigationState? PopPendingNavigation()
        {
            var rawNavigationMode = HttpContext.Session.GetString(PendingNavigationModeSessionKey);
            var rawCursorCategoryId = HttpContext.Session.GetString(PendingNavigationCursorCategoryIdSessionKey);
            ClearPendingNavigation();

            if (!TryResolveNavigationMode(rawNavigationMode, out var normalizedNavigationMode))
            {
                return null;
            }

            if (!long.TryParse(rawCursorCategoryId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cursorCategoryId) || cursorCategoryId <= 0)
            {
                return null;
            }

            return new PendingNavigationState(
                string.Equals(normalizedNavigationMode, NavigationModeNext, StringComparison.Ordinal),
                cursorCategoryId);
        }

        private void ClearPendingNavigation()
        {
            HttpContext.Session.Remove(PendingNavigationModeSessionKey);
            HttpContext.Session.Remove(PendingNavigationCursorCategoryIdSessionKey);
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

        private readonly struct PendingNavigationState(bool isNextPage, long cursorCategoryId)
        {
            public bool IsNextPage { get; } = isNextPage;
            public long CursorCategoryId { get; } = cursorCategoryId;
        }
    }
}
