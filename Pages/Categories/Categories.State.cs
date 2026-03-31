using System.Globalization;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel
    {
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

        private void SetSortState(string sortBy, string sortDirection)
        {
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
            if (!currentPageInSession.HasValue || currentPageInSession.Value <= 0)
            {
                CurrentPage = 1;
            }
            else
            {
                CurrentPage = currentPageInSession.Value;
            }

            var rawAnchorCategoryId = HttpContext.Session.GetString(CurrentAnchorCategoryIdSessionKey);
            if (!long.TryParse(rawAnchorCategoryId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAnchorCategoryId) || parsedAnchorCategoryId < 0)
            {
                CurrentAnchorCategoryId = 0;
            }
            else
            {
                CurrentAnchorCategoryId = parsedAnchorCategoryId;
            }

            LoadSortStateFromSession();
        }

        private void SaveStateInSession()
        {
            HttpContext.Session.SetInt32(CurrentPageSessionKey, CurrentPage > 0 ? CurrentPage : 1);
            HttpContext.Session.SetString(CurrentAnchorCategoryIdSessionKey, Math.Max(CurrentAnchorCategoryId, 0).ToString(CultureInfo.InvariantCulture));
            HttpContext.Session.SetString(SortBySessionKey, NormalizeSortBy(SortBy));
            HttpContext.Session.SetString(SortDirectionSessionKey, NormalizeSortDirection(SortDirection));
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
        }

        private void LoadSortStateFromSession()
        {
            var sortByInSession = HttpContext.Session.GetString(SortBySessionKey);
            var sortDirectionInSession = HttpContext.Session.GetString(SortDirectionSessionKey);

            SortBy = NormalizeSortBy(sortByInSession is string persistedSortBy ? persistedSortBy : string.Empty);
            SortDirection = NormalizeSortDirection(sortDirectionInSession is string persistedSortDirection ? persistedSortDirection : string.Empty);
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
                "id" => "id",
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
    }
}
