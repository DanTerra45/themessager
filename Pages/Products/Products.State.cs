using System.Globalization;

namespace Mercadito.Pages.Products
{
    public partial class ProductsModel
    {
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

        private void EnsureDefaultNewProductValues()
        {
            if (string.IsNullOrWhiteSpace(NewProduct.Batch))
            {
                NewProduct.Batch = string.Empty;
            }

            if (!NewProduct.Stock.HasValue || NewProduct.Stock.Value < 0)
            {
                NewProduct.Stock = 0;
            }

            if (NewProduct.ExpirationDate == default)
            {
                NewProduct.ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3));
            }

            if (!NewProduct.Price.HasValue || NewProduct.Price.Value < 0.01m)
            {
                NewProduct.Price = 0.01m;
            }
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

            var rawCategoryFilter = HttpContext.Session.GetString(CategoryFilterSessionKey);
            if (!long.TryParse(rawCategoryFilter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCategoryFilter) || parsedCategoryFilter < 0)
            {
                CategoryFilter = 0;
            }
            else
            {
                CategoryFilter = parsedCategoryFilter;
            }

            var persistedSearchTerm = HttpContext.Session.GetString(SearchTermSessionKey);
            SearchTerm = NormalizeSearchTerm(persistedSearchTerm is string sessionSearchTerm ? sessionSearchTerm : string.Empty);

            var rawAnchorProductId = HttpContext.Session.GetString(CurrentAnchorProductIdSessionKey);
            if (!long.TryParse(rawAnchorProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAnchorProductId) || parsedAnchorProductId < 0)
            {
                CurrentAnchorProductId = 0;
            }
            else
            {
                CurrentAnchorProductId = parsedAnchorProductId;
            }

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
            var sortByInSession = HttpContext.Session.GetString(SortBySessionKey);
            var sortDirectionInSession = HttpContext.Session.GetString(SortDirectionSessionKey);
            SortBy = NormalizeSortBy(sortByInSession is string persistedSortBy ? persistedSortBy : string.Empty);
            SortDirection = NormalizeSortDirection(sortDirectionInSession is string persistedSortDirection ? persistedSortDirection : string.Empty);
            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
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
            if (string.IsNullOrWhiteSpace(normalizedOrderPreset))
            {
                OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
                return;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetCustom, StringComparison.Ordinal))
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

        private static string NormalizeOrderPreset(string orderPreset)
        {
            if (string.IsNullOrWhiteSpace(orderPreset))
            {
                return string.Empty;
            }

            var normalizedOrderPreset = orderPreset.Trim().ToLowerInvariant();
            if (string.Equals(normalizedOrderPreset, OrderPresetRecent, StringComparison.Ordinal))
            {
                return OrderPresetRecent;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalAsc, StringComparison.Ordinal))
            {
                return OrderPresetAlphabeticalAsc;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetAlphabeticalDesc, StringComparison.Ordinal))
            {
                return OrderPresetAlphabeticalDesc;
            }

            if (string.Equals(normalizedOrderPreset, OrderPresetCustom, StringComparison.Ordinal))
            {
                return OrderPresetCustom;
            }

            return string.Empty;
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
                return NormalizeSearchTerm(persistedSearchTerm is string sessionSearchTerm ? sessionSearchTerm : string.Empty);
            }

            return NormalizeSearchTerm(searchTerm);
        }

        private static string NormalizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return string.Empty;
            }

            return searchTerm.Trim();
        }
    }
}
