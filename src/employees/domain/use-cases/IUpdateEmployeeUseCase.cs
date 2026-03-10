using Mercadito.src.employees.domain.dto;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IUpdateEmployeeUseCase
    {
        Task ExecuteAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default);
    }
}
