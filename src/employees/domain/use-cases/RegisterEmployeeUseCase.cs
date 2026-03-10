using Mercadito.src.employees.data.dto;
using Mercadito.src.employees.domain.repository;
using Microsoft.Extensions.Logging;

namespace Mercadito.src.employees.domain.usecases
{
    #pragma warning disable S2139 // Permite loggear y relanzar excepciones
    public class RegisterEmployeeUseCase : IRegisterEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<RegisterEmployeeUseCase> _logger;

        public RegisterEmployeeUseCase(IEmployeeRepository employeeRepository, ILogger<RegisterEmployeeUseCase> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<long> ExecuteAsync(CreateEmployeeDto employee)
        {
            try
            {
                return await _employeeRepository.AddEmployeeAsync(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en caso de uso al registrar empleado");
                throw;
            }
        }
    }
    #pragma warning restore S2139
}