using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mercadito
{
    #pragma warning disable S2139
    public class UpdateEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<UpdateEmployeeUseCase> _logger;

        public UpdateEmployeeUseCase(IEmployeeRepository employeeRepository, ILogger<UpdateEmployeeUseCase> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task ExecuteAsync(Employee employee)
        {
            try
            {
                await _employeeRepository.UpdateEmployeeAsync(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en caso de uso al actualizar empleado");
                throw;
            }
        }
    }
    #pragma warning restore S2139
}