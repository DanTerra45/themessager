using Mercadito.src.employees.data.dto;

namespace Mercadito.src.employees.domain.usecases
{
    public interface IUpdateEmployeeUseCase
    {
        Task ExecuteAsync(UpdateEmployeeDto employee);
    }
}
