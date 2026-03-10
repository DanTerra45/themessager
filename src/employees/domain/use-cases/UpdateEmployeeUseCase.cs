using Mercadito.src.employees.data.dto;
using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.repository;

namespace Mercadito.src.employees.domain.usecases
{
    public class UpdateEmployeeUseCase : IUpdateEmployeeUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<UpdateEmployeeUseCase> _logger;

        public UpdateEmployeeUseCase(IEmployeeRepository employeeRepository, ILogger<UpdateEmployeeUseCase> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task ExecuteAsync(UpdateEmployeeDto employee)
        {
            try
            {
                var current = await _employeeRepository.GetEmployeeByIdAsync(employee.Id);
                if (current is null)
                {
                    throw new InvalidOperationException("Empleado no encontrado.");
                }

                var employeeToUpdate = new Employee
                {
                    Id = employee.Id,
                    Ci = employee.Ci,
                    Complemento = employee.Complemento ?? string.Empty,
                    Nombres = employee.Nombres,
                    PrimerApellido = employee.PrimerApellido,
                    SegundoApellido = employee.SegundoApellido ?? string.Empty,
                    Rol = employee.Rol,
                    NumeroContacto = employee.NumeroContacto,
                    IsActive = employee.IsActive
                };

                await _employeeRepository.UpdateEmployeeAsync(employeeToUpdate);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error en caso de uso al actualizar empleado");
                throw;
            }
        }
    }
}