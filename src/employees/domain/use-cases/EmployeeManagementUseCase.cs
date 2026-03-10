using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class EmployeeManagementUseCase(IEmployeeRepository employeeRepository) : IEmployeeManagementUseCase
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<(IReadOnlyList<Employee> Employees, int TotalPages)> GetPageAsync(int currentPage, int pageSize)
        {
            var totalCount = await _employeeRepository.GetTotalEmployeesCountAsync();
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var employees = (await _employeeRepository.GetEmployeesByPages(currentPage, pageSize)).ToList();
            return (employees, totalPages);
        }

        public async Task<Employee?> GetForEditAsync(long employeeId)
        {
            return await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        }

        public async Task<bool> DeleteAsync(long employeeId)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            if (employee is null)
            {
                return false;
            }

            await _employeeRepository.DeleteEmployeeAsync(employeeId);
            return true;

        }

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
        }
    }
}
