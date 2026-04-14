using Mercadito.Pages.Infrastructure;
using Mercadito.src.domain.shared.exceptions;

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
                    loadedEmployees = [.. await _employeeManagementUseCase.GetPageByCursorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorEmployeeId,
                        isNextPage: false,
                        SearchTerm,
                        cancellationToken)];

                    if (loadedEmployees.Count > 0 && CurrentPage > 1)
                    {
                        CurrentPage--;
                    }
                }

                if (loadedEmployees.Count == 0 && CurrentAnchorEmployeeId > 0)
                {
                    CurrentAnchorEmployeeId = 0;
                    CurrentPage = 1;
                    loadedEmployees = [.. await _employeeManagementUseCase.GetPageFromAnchorAsync(
                        _defaultPageSize,
                        SortBy,
                        SortDirection,
                        CurrentAnchorEmployeeId,
                        SearchTerm,
                        cancellationToken)];
                }

                Employees = loadedEmployees;
                CurrentAnchorEmployeeId = _listingPageStateService.ResolveCurrentAnchorId(Employees, employee => employee.Id);
                await UpdateNavigationFlagsAsync();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar empleados");
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
                CurrentAnchorEmployeeId = _listingPageStateService.ResolveCurrentAnchorId(Employees, employee => employee.Id);
                CurrentPage = _listingPageStateService.MoveCurrentPage(CurrentPage, isNextPage);

                await UpdateNavigationFlagsAsync();
            }
            catch (DataStoreUnavailableException exception)
            {
                _logger.LogError(exception, "Base de datos no disponible al cargar empleados con cursor");
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
            _listingPageStateService.SetPendingNavigation(HttpContext.Session, ListingSessionKeys, navigationMode, cursorEmployeeId);
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
