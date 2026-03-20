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

        public async Task<(IReadOnlyList<EmployeeModel> Employees, int TotalPages)> GetPageAsync(
            int currentPage,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            var employeeRepository = _employeeRepositoryCreator.Create();
            var totalCount = await employeeRepository.GetTotalEmployeesCountAsync(cancellationToken);
            var totalPages = CalculateTotalPages(totalCount, pageSize);
            var employees = await employeeRepository.GetEmployeesByPages(currentPage, pageSize, sortBy, sortDirection, cancellationToken);
            return (employees, totalPages);
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

        private static int CalculateTotalPages(int totalItems, int pageSize)
        {
            if (totalItems == 0 || pageSize <= 0) return 1;
            return (totalItems + pageSize - 1) / pageSize;
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
