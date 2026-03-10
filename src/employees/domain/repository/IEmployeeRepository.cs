using Mercadito.src.employees.data.dto;
using Mercadito.src.employees.data.entity;

namespace Mercadito.src.employees.domain.repository
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<IEnumerable<Employee>> GetEmployeesByPages(int page, int pageSize = 10);
        Task<int> GetTotalEmployeesCountAsync();
        Task<Employee?> GetEmployeeByIdAsync(long id);
        Task<long> AddEmployeeAsync(CreateEmployeeDto employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(long id);
    }
}