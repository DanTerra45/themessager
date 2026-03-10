using Mercadito.src.employees.data.dto;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IRegisterEmployeeUseCase
    {
        Task<long> ExecuteAsync(CreateEmployeeDto employee);
    }
}
