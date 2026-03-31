using System.Globalization;
using MySqlConnector;

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
                    loadedProducts = (await _productManagementUseCase.GetPageFromAnchorAsync(
                        CategoryFilter,
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorProductId,
                        SearchTerm,
                        cancellationToken)).ToList();
                }

                Products = loadedProducts;
                CurrentAnchorProductId = Products.Count > 0 ? Products[0].Id : 0;
                await UpdateNavigationFlagsAsync();
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar productos.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar productos.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar productos");
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
                CurrentAnchorProductId = Products.Count > 0 ? Products[0].Id : 0;
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
                _logger.LogError(exception, "Base de datos no disponible al cargar productos con cursor.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar productos con cursor.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar productos con cursor");
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
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para productos.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías para productos.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías");
                Categories = [];
            }
        }

        private void SetPendingNavigation(string navigationMode, long cursorProductId)
        {
            if (cursorProductId <= 0 || !TryResolveNavigationMode(navigationMode, out var normalizedNavigationMode))
            {
                ClearPendingNavigation();
                return;
            }

            HttpContext.Session.SetString(PendingNavigationModeSessionKey, normalizedNavigationMode);
            HttpContext.Session.SetString(PendingNavigationCursorProductIdSessionKey, cursorProductId.ToString(CultureInfo.InvariantCulture));
        }

        private PendingNavigationState? PopPendingNavigation()
        {
            var rawNavigationMode = HttpContext.Session.GetString(PendingNavigationModeSessionKey);
            var rawCursorProductId = HttpContext.Session.GetString(PendingNavigationCursorProductIdSessionKey);
            ClearPendingNavigation();

            if (!TryResolveNavigationMode(rawNavigationMode, out var normalizedNavigationMode))
            {
                return null;
            }

            if (!long.TryParse(rawCursorProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cursorProductId) || cursorProductId <= 0)
            {
                return null;
            }

            return new PendingNavigationState(
                string.Equals(normalizedNavigationMode, NavigationModeNext, StringComparison.Ordinal),
                cursorProductId);
        }

        private void ClearPendingNavigation()
        {
            HttpContext.Session.Remove(PendingNavigationModeSessionKey);
            HttpContext.Session.Remove(PendingNavigationCursorProductIdSessionKey);
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

        private readonly struct PendingNavigationState(bool isNextPage, long cursorProductId)
        {
            public bool IsNextPage { get; } = isNextPage;
            public long CursorProductId { get; } = cursorProductId;
        }
    }
}
