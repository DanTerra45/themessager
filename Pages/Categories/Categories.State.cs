using Mercadito.Pages.Infrastructure;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel
    {
        private void SetSearchAndSortState(string searchTerm, string sortBy, string sortDirection)
        {
            SearchTerm = ResolveSearchTermFromRequest(searchTerm);
            (SortBy, SortDirection) = _listingPageStateService.ResolveSortState(HttpContext.Session, ListingStateOptions, sortBy, sortDirection);
        }

        public string GetSortIcon(string columnName)
        {
            return _listingPageStateService.GetSortIcon(SortBy, SortDirection, columnName, ListingStateOptions);
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
                "id" => "id",
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
    }
}
