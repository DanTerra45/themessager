using System.Globalization;

namespace Mercadito.Pages.Infrastructure
{
    public readonly record struct KeysetListingSessionKeys(
        string CurrentPageKey,
        string CurrentAnchorKey,
        string PendingNavigationModeKey,
        string PendingNavigationCursorKey,
        string SortByKey,
        string SortDirectionKey,
        string SearchTermKey);

    public readonly record struct KeysetListingSessionState(
        int CurrentPage,
        long CurrentAnchorId,
        string SortBy,
        string SortDirection,
        string SearchTerm);

    public readonly record struct KeysetPendingNavigationState(bool IsNextPage, long CursorId);

    public readonly record struct ListingPageStateOptions(
        KeysetListingSessionKeys SessionKeys,
        string DefaultSortBy,
        string DefaultSortDirection,
        Func<string, string> NormalizeSortBy,
        Func<string, string> NormalizeSortDirection,
        Func<string, string> NormalizeSearchTerm);

    public interface IListingPageStateService
    {
        KeysetListingSessionState LoadState(ISession session, ListingPageStateOptions options);
        (string SortBy, string SortDirection) LoadSortState(ISession session, ListingPageStateOptions options);
        (string SortBy, string SortDirection) ResolveSortState(ISession session, ListingPageStateOptions options, string sortBy, string sortDirection);
        void SaveState(ISession session, KeysetListingSessionState state, ListingPageStateOptions options);
        KeysetListingSessionState NormalizeState(KeysetListingSessionState state, ListingPageStateOptions options);
        string ResolveSearchTermFromRequest(HttpRequest request, ISession session, ListingPageStateOptions options, string searchTerm);
        string GetSortIcon(string currentSortBy, string currentSortDirection, string columnName, ListingPageStateOptions options);
        (string SortBy, string SortDirection) ToggleSort(string currentSortBy, string currentSortDirection, string nextSortBy, ListingPageStateOptions options);
        void SetPendingNavigation(ISession session, KeysetListingSessionKeys keys, string navigationMode, long cursorId);
        KeysetPendingNavigationState? PopPendingNavigation(ISession session, KeysetListingSessionKeys keys);
        void ClearPendingNavigation(ISession session, KeysetListingSessionKeys keys);
        long LoadNonNegativeLong(ISession session, string key);
        void SaveNonNegativeLong(ISession session, string key, long value);
        long ResolveCurrentAnchorId<TItem>(IReadOnlyList<TItem> items, Func<TItem, long> idSelector);
        int MoveCurrentPage(int currentPage, bool isNextPage);
    }

    public sealed class ListingPageStateService : IListingPageStateService
    {
        private const string NavigationModeNext = "next";
        private const string NavigationModePrevious = "prev";

        public KeysetListingSessionState LoadState(ISession session, ListingPageStateOptions options)
        {
            ArgumentNullException.ThrowIfNull(session);

            var currentPageInSession = session.GetInt32(options.SessionKeys.CurrentPageKey);
            var currentPage = 1;
            if (currentPageInSession.HasValue && currentPageInSession.Value > 0)
            {
                currentPage = currentPageInSession.Value;
            }

            var currentAnchorId = LoadNonNegativeLong(session, options.SessionKeys.CurrentAnchorKey);

            var rawSortBy = session.GetString(options.SessionKeys.SortByKey);
            if (rawSortBy == null)
            {
                rawSortBy = options.DefaultSortBy;
            }

            var rawSortDirection = session.GetString(options.SessionKeys.SortDirectionKey);
            if (rawSortDirection == null)
            {
                rawSortDirection = options.DefaultSortDirection;
            }

            var rawSearchTerm = session.GetString(options.SessionKeys.SearchTermKey);
            if (rawSearchTerm == null)
            {
                rawSearchTerm = string.Empty;
            }

            var sortBy = options.NormalizeSortBy(rawSortBy);
            var sortDirection = options.NormalizeSortDirection(rawSortDirection);
            var searchTerm = options.NormalizeSearchTerm(rawSearchTerm);

            return new KeysetListingSessionState(currentPage, currentAnchorId, sortBy, sortDirection, searchTerm);
        }

        public (string SortBy, string SortDirection) LoadSortState(ISession session, ListingPageStateOptions options)
        {
            ArgumentNullException.ThrowIfNull(session);

            var rawSortBy = session.GetString(options.SessionKeys.SortByKey);
            if (rawSortBy == null)
            {
                rawSortBy = options.DefaultSortBy;
            }

            var rawSortDirection = session.GetString(options.SessionKeys.SortDirectionKey);
            if (rawSortDirection == null)
            {
                rawSortDirection = options.DefaultSortDirection;
            }

            var sortBy = options.NormalizeSortBy(rawSortBy);
            var sortDirection = options.NormalizeSortDirection(rawSortDirection);
            return (sortBy, sortDirection);
        }

        public (string SortBy, string SortDirection) ResolveSortState(ISession session, ListingPageStateOptions options, string sortBy, string sortDirection)
        {
            ArgumentNullException.ThrowIfNull(session);

            if (string.IsNullOrWhiteSpace(sortBy) && string.IsNullOrWhiteSpace(sortDirection))
            {
                return LoadSortState(session, options);
            }

            return (options.NormalizeSortBy(sortBy), options.NormalizeSortDirection(sortDirection));
        }

        public void SaveState(ISession session, KeysetListingSessionState state, ListingPageStateOptions options)
        {
            ArgumentNullException.ThrowIfNull(session);

            var currentPageToPersist = 1;
            if (state.CurrentPage > 0)
            {
                currentPageToPersist = state.CurrentPage;
            }

            session.SetInt32(options.SessionKeys.CurrentPageKey, currentPageToPersist);
            SaveNonNegativeLong(session, options.SessionKeys.CurrentAnchorKey, state.CurrentAnchorId);
            session.SetString(options.SessionKeys.SortByKey, options.NormalizeSortBy(state.SortBy));
            session.SetString(options.SessionKeys.SortDirectionKey, options.NormalizeSortDirection(state.SortDirection));
            session.SetString(options.SessionKeys.SearchTermKey, options.NormalizeSearchTerm(state.SearchTerm));
        }

        public KeysetListingSessionState NormalizeState(KeysetListingSessionState state, ListingPageStateOptions options)
        {
            var currentPage = 1;
            if (state.CurrentPage > 0)
            {
                currentPage = state.CurrentPage;
            }

            var currentAnchorId = 0L;
            if (state.CurrentAnchorId >= 0)
            {
                currentAnchorId = state.CurrentAnchorId;
            }

            if (currentAnchorId == 0)
            {
                currentPage = 1;
            }

            return new KeysetListingSessionState(
                currentPage,
                currentAnchorId,
                options.NormalizeSortBy(state.SortBy),
                options.NormalizeSortDirection(state.SortDirection),
                options.NormalizeSearchTerm(state.SearchTerm));
        }

        public string ResolveSearchTermFromRequest(HttpRequest request, ISession session, ListingPageStateOptions options, string searchTerm)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(session);

            var hasSearchTermInForm = request.HasFormContentType && request.Form.ContainsKey("searchTerm");
            var hasSearchTermInQuery = request.Query.ContainsKey("searchTerm");

            if (hasSearchTermInForm || hasSearchTermInQuery)
            {
                return options.NormalizeSearchTerm(searchTerm);
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var sessionSearchTerm = session.GetString(options.SessionKeys.SearchTermKey);
                if (sessionSearchTerm == null)
                {
                    sessionSearchTerm = string.Empty;
                }

                return options.NormalizeSearchTerm(sessionSearchTerm);
            }

            return options.NormalizeSearchTerm(searchTerm);
        }

        public string GetSortIcon(string currentSortBy, string currentSortDirection, string columnName, ListingPageStateOptions options)
        {
            var normalizedColumn = options.NormalizeSortBy(columnName);
            if (!string.Equals(currentSortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return "bi-arrow-down-up";
            }

            if (string.Equals(currentSortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                return "bi-sort-down";
            }

            return "bi-sort-up";
        }

        public (string SortBy, string SortDirection) ToggleSort(string currentSortBy, string currentSortDirection, string nextSortBy, ListingPageStateOptions options)
        {
            var normalizedSortBy = options.NormalizeSortBy(nextSortBy);
            if (string.Equals(currentSortBy, normalizedSortBy, StringComparison.OrdinalIgnoreCase))
            {
                var nextSortDirection = "asc";
                if (string.Equals(currentSortDirection, "asc", StringComparison.OrdinalIgnoreCase))
                {
                    nextSortDirection = "desc";
                }

                return (currentSortBy, nextSortDirection);
            }

            return (normalizedSortBy, options.DefaultSortDirection);
        }

        public void SetPendingNavigation(ISession session, KeysetListingSessionKeys keys, string navigationMode, long cursorId)
        {
            ArgumentNullException.ThrowIfNull(session);

            if (cursorId <= 0 || !TryResolveNavigationMode(navigationMode, out var normalizedNavigationMode))
            {
                ClearPendingNavigation(session, keys);
                return;
            }

            session.SetString(keys.PendingNavigationModeKey, normalizedNavigationMode);
            session.SetString(keys.PendingNavigationCursorKey, cursorId.ToString(CultureInfo.InvariantCulture));
        }

        public KeysetPendingNavigationState? PopPendingNavigation(ISession session, KeysetListingSessionKeys keys)
        {
            ArgumentNullException.ThrowIfNull(session);

            var rawNavigationMode = session.GetString(keys.PendingNavigationModeKey);
            var rawCursorId = session.GetString(keys.PendingNavigationCursorKey);
            ClearPendingNavigation(session, keys);

            if (!TryResolveNavigationMode(rawNavigationMode, out var normalizedNavigationMode))
            {
                return null;
            }

            if (!long.TryParse(rawCursorId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cursorId) || cursorId <= 0)
            {
                return null;
            }

            return new KeysetPendingNavigationState(
                string.Equals(normalizedNavigationMode, NavigationModeNext, StringComparison.Ordinal),
                cursorId);
        }

        public void ClearPendingNavigation(ISession session, KeysetListingSessionKeys keys)
        {
            ArgumentNullException.ThrowIfNull(session);

            session.Remove(keys.PendingNavigationModeKey);
            session.Remove(keys.PendingNavigationCursorKey);
        }

        public long LoadNonNegativeLong(ISession session, string key)
        {
            ArgumentNullException.ThrowIfNull(session);

            var rawValue = session.GetString(key);
            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) && parsedValue >= 0)
            {
                return parsedValue;
            }

            return 0;
        }

        public void SaveNonNegativeLong(ISession session, string key, long value)
        {
            ArgumentNullException.ThrowIfNull(session);

            session.SetString(key, Math.Max(value, 0).ToString(CultureInfo.InvariantCulture));
        }

        public long ResolveCurrentAnchorId<TItem>(IReadOnlyList<TItem> items, Func<TItem, long> idSelector)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(idSelector);

            if (items.Count == 0)
            {
                return 0;
            }

            return idSelector(items[0]);
        }

        public int MoveCurrentPage(int currentPage, bool isNextPage)
        {
            if (isNextPage)
            {
                return currentPage + 1;
            }

            if (currentPage > 1)
            {
                return currentPage - 1;
            }

            return 1;
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
    }
}
