using Mercadito.Pages.Infrastructure;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.Pages.Products
{
    public partial class ProductsModel
    {
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
            ListingState = _listingPageStateService.LoadState(HttpContext.Session, ListingStateOptions);
            CategoryFilter = _listingPageStateService.LoadNonNegativeLong(HttpContext.Session, CategoryFilterSessionKey);

            OrderPreset = ResolveOrderPreset(SortBy, SortDirection);
        }

        private void SaveStateInSession()
        {
            _listingPageStateService.SaveState(HttpContext.Session, ListingState, ListingStateOptions);
            _listingPageStateService.SaveNonNegativeLong(HttpContext.Session, CategoryFilterSessionKey, CategoryFilter);
        }

        public string GetSortIcon(string columnName)
        {
            return _listingPageStateService.GetSortIcon(SortBy, SortDirection, columnName, ListingStateOptions);
        }

        private void ToggleSort(string sortBy)
        {
            (SortBy, SortDirection) = _listingPageStateService.ToggleSort(SortBy, SortDirection, sortBy, ListingStateOptions);
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

            var normalizedOrderPreset = ValidationText.NormalizeLowerTrimmed(orderPreset);
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
    }
}
