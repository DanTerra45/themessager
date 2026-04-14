using Mercadito.Pages.Infrastructure;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.Pages.Categories
{
    public partial class CategoriesModel
    {
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
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías.");
                ModelState.AddModelError(string.Empty, "Error al cargar las categorías. Intente nuevamente.");
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
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías con cursor.");
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

        private async Task LoadNextCategoryCodePreviewAsync()
        {
            try
            {
                NextCategoryCodePreview = await _categoryManagementUseCase.GetNextCategoryCodePreviewAsync(HttpContext.RequestAborted);
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al generar vista previa de código de categoría.");
                NextCategoryCodePreview = "C00001";
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
