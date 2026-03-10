using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mercadito
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<IEnumerable<Employee>> GetEmployeesByPages(int page, int pageSize = 10);
        Task<Employee?> GetEmployeeByIdAsync(long id);          
        Task<long> AddEmployeeAsync(CreateEmployeeDto employee); 
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(long id);              
    }
}