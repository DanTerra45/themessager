using Mercadito.src.employees.data.repository;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.employees.domain.model;
using Mercadito.src.shared.domain.factory;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.employees.domain.usecases
{
    public class EmployeeManagementUseCase(
        RepositoryCreator<EmployeeRepository> employeeRepositoryCreator,
        IEmployeeFactory employeeFactory) : IEmployeeManagementUseCase
    {
        private readonly RepositoryCreator<EmployeeRepository> _employeeRepositoryCreator = employeeRepositoryCreator;
        private readonly IEmployeeFactory _employeeFactory = employeeFactory;

        public async Task<IReadOnlyList<EmployeeModel>> GetPageByCursorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            return await employeeRepository.GetEmployeesByCursorAsync(
                pageSize,
                sortBy,
                sortDirection,
                cursorEmployeeId,
                isNextPage,
                cancellationToken);
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetPageFromAnchorAsync(
            int pageSize,
            string sortBy,
            string sortDirection,
            long anchorEmployeeId,
            CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            return await employeeRepository.GetEmployeesFromAnchorAsync(
                pageSize,
                sortBy,
                sortDirection,
                anchorEmployeeId,
                cancellationToken);
        }

        public async Task<bool> HasEmployeesByCursorAsync(
            string sortBy,
            string sortDirection,
            long cursorEmployeeId,
            bool isNextPage,
            CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            return await employeeRepository.HasEmployeesByCursorAsync(
                sortBy,
                sortDirection,
                cursorEmployeeId,
                isNextPage,
                cancellationToken);
        }

        public async Task<UpdateEmployeeDto?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null)
            {
                return null;
            }

            return new UpdateEmployeeDto
            {
                Id = employee.Id,
                Ci = employee.Ci,
                Complemento = employee.Complemento,
                Nombres = employee.Nombres,
                PrimerApellido = employee.PrimerApellido,
                SegundoApellido = employee.SegundoApellido,
                NumeroContacto = NormalizeContactForUi(employee.NumeroContacto),
                Rol = employee.Rol
            };
        }

        public async Task CreateAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var employeeToCreate = _employeeFactory.CreateForInsert(employee);
            await employeeRepository.CreateAsync(employeeToCreate, cancellationToken);
        }

        public async Task UpdateAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var employeeToUpdate = _employeeFactory.CreateForUpdate(employee);

            var affectedRows = await employeeRepository.UpdateAsync(employeeToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                throw new ValidationException("Empleado no encontrado.");
            }
        }

        public async Task<bool> DeleteAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var affectedRows = await employeeRepository.DeleteAsync(employeeId, cancellationToken);
            return affectedRows > 0;
        }

        private static string NormalizeContactForUi(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var digitsOnly = new List<char>(11);
            foreach (var character in value)
            {
                if (char.IsDigit(character))
                {
                    digitsOnly.Add(character);
                }
            }

            if (digitsOnly.Count >= 8)
            {
                return new string(digitsOnly.GetRange(digitsOnly.Count - 8, 8).ToArray());
            }

            return new string([.. digitsOnly]);
        }
    }
}
