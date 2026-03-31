using System.Globalization;
using MySqlConnector;

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
                    cancellationToken)).ToList();

                if (loadedCategories.Count == 0 && CurrentAnchorCategoryId > 0)
                {
                    loadedCategories = (await _categoryManagementUseCase.GetPageByCursorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorCategoryId,
                        isNextPage: false,
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
                        cancellationToken)).ToList();
                }

                Categories = loadedCategories;
                CurrentAnchorCategoryId = Categories.Count > 0 ? Categories[0].Id : 0;
                await UpdateNavigationFlagsAsync();
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías");
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
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías con cursor.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar categorías con cursor.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar categorías con cursor");
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
                HttpContext.RequestAborted);

            HasNextPage = await _categoryManagementUseCase.HasCategoriesByCursorAsync(
                SortBy,
                SortDirection,
                lastCategoryId,
                isNextPage: true,
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
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al generar vista previa de código de categoría.");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al generar vista previa de código de categoría.");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "No se pudo obtener la vista previa del próximo código de categoría");
                NextCategoryCodePreview = "C00001";
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
