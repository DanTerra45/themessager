using Mercadito.src.employees.domain.dto;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IRegisterEmployeeUseCase
    {
        Task<long> ExecuteAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default);
    }
}
