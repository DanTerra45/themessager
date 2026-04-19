using Mercadito.Pages.Infrastructure;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.Pages.Products
{
    public partial class ProductsModel
    {
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
                _logger.LogError(exception, "Base de datos no disponible al cargar productos.");
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
                _logger.LogError(exception, "Base de datos no disponible al cargar productos con cursor.");
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
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para productos.");
                Categories = [];
            }
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
    }
}
