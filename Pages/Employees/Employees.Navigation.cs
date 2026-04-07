using System.Globalization;
using MySqlConnector;

namespace Mercadito.Pages.Employees
{
    public partial class EmployeesModel
    {
        private async Task LoadEmployeesFromAnchorAsync()
        {
            try
            {
                var cancellationToken = HttpContext.RequestAborted;
                var loadedEmployees = (await _employeeManagementUseCase.GetPageFromAnchorAsync(
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    CurrentAnchorEmployeeId,
                    SearchTerm,
                    cancellationToken)).ToList();

                if (loadedEmployees.Count == 0 && CurrentAnchorEmployeeId > 0)
                {
                    loadedEmployees = (await _employeeManagementUseCase.GetPageByCursorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorEmployeeId,
                        isNextPage: false,
                        SearchTerm,
                        cancellationToken)).ToList();

                    if (loadedEmployees.Count > 0 && CurrentPage > 1)
                    {
                        CurrentPage--;
                    }
                }

                if (loadedEmployees.Count == 0 && CurrentAnchorEmployeeId > 0)
                {
                    CurrentAnchorEmployeeId = 0;
                    CurrentPage = 1;
                    loadedEmployees = (await _employeeManagementUseCase.GetPageFromAnchorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorEmployeeId,
                        SearchTerm,
                        cancellationToken)).ToList();
                }

                Employees = loadedEmployees;
                CurrentAnchorEmployeeId = Employees.Count > 0 ? Employees[0].Id : 0;
                await UpdateNavigationFlagsAsync();
            }
            catch (MySqlException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar empleados");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar empleados");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar empleados");
                ModelState.AddModelError(string.Empty, "Error al cargar los empleados.");
                Employees = [];
                HasPreviousPage = false;
                HasNextPage = false;
                CurrentAnchorEmployeeId = 0;
                CurrentPage = 1;
            }
        }

        private async Task LoadEmployeesByCursorAsync(bool isNextPage, long cursorEmployeeId)
        {
            if (cursorEmployeeId <= 0)
            {
                await LoadEmployeesFromAnchorAsync();
                return;
            }

            try
            {
                var employees = await _employeeManagementUseCase.GetPageByCursorAsync(
                    _defaultPageSize,
                    SortBy,
                    SortDirection,
                    cursorEmployeeId,
                    isNextPage,
                    SearchTerm,
                    HttpContext.RequestAborted);

                if (employees.Count == 0)
                {
                    await LoadEmployeesFromAnchorAsync();
                    return;
                }

                Employees = [.. employees];
                CurrentAnchorEmployeeId = Employees.Count > 0 ? Employees[0].Id : 0;
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
                _logger.LogError(exception, "Base de datos no disponible al cargar empleados con cursor");
                throw;
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar empleados con cursor");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error al cargar empleados con cursor");
                await LoadEmployeesFromAnchorAsync();
            }
        }

        private async Task UpdateNavigationFlagsAsync()
        {
            if (Employees.Count == 0)
            {
                HasPreviousPage = false;
                HasNextPage = false;
                return;
            }

            var firstEmployeeId = Employees[0].Id;
            var lastEmployeeId = Employees[Employees.Count - 1].Id;

            HasPreviousPage = await _employeeManagementUseCase.HasEmployeesByCursorAsync(
                SortBy,
                SortDirection,
                firstEmployeeId,
                isNextPage: false,
                SearchTerm,
                HttpContext.RequestAborted);

            HasNextPage = await _employeeManagementUseCase.HasEmployeesByCursorAsync(
                SortBy,
                SortDirection,
                lastEmployeeId,
                isNextPage: true,
                SearchTerm,
                HttpContext.RequestAborted);

            if (CurrentPage <= 1)
            {
                HasPreviousPage = false;
            }
        }

        private void SetPendingNavigation(string navigationMode, long cursorEmployeeId)
        {
            if (cursorEmployeeId <= 0 || !TryResolveNavigationMode(navigationMode, out var normalizedNavigationMode))
            {
                ClearPendingNavigation();
                return;
            }

            HttpContext.Session.SetString(PendingNavigationModeSessionKey, normalizedNavigationMode);
            HttpContext.Session.SetString(PendingNavigationCursorEmployeeIdSessionKey, cursorEmployeeId.ToString(CultureInfo.InvariantCulture));
        }

        private PendingNavigationState? PopPendingNavigation()
        {
            var rawNavigationMode = HttpContext.Session.GetString(PendingNavigationModeSessionKey);
            var rawCursorEmployeeId = HttpContext.Session.GetString(PendingNavigationCursorEmployeeIdSessionKey);
            ClearPendingNavigation();

            if (!TryResolveNavigationMode(rawNavigationMode, out var normalizedNavigationMode))
            {
                return null;
            }

            if (!long.TryParse(rawCursorEmployeeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cursorEmployeeId) || cursorEmployeeId <= 0)
            {
                return null;
            }

            return new PendingNavigationState(
                string.Equals(normalizedNavigationMode, NavigationModeNext, StringComparison.Ordinal),
                cursorEmployeeId);
        }

        private void ClearPendingNavigation()
        {
            HttpContext.Session.Remove(PendingNavigationModeSessionKey);
            HttpContext.Session.Remove(PendingNavigationCursorEmployeeIdSessionKey);
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

        private readonly struct PendingNavigationState(bool isNextPage, long cursorEmployeeId)
        {
            public bool IsNextPage { get; } = isNextPage;
            public long CursorEmployeeId { get; } = cursorEmployeeId;
        }
    }
}
