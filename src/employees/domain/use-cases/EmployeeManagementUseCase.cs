using Mercadito.src.employees.data.repository;
using Mercadito.src.employees.domain.dto;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.employees.domain.model;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mercadito.src.shared.domain.factory;
using Mercadito.src.employees.data.entity;

namespace Mercadito.src.employees.domain.usecases
{
    // keep the terse primary-constructor style used previously
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

        public async Task<Result> CreateAsync(CreateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            Employee employeeToCreate;
            try
            {
                employeeToCreate = _employeeFactory.CreateForInsert(employee);
            }
            catch (ValidationException ex)
            {
                return Result.Failure(ex.Message);
            }

            var employeeRepository = _employeeRepositoryCreator.Create();

            try
            {
                await employeeRepository.CreateAsync(employeeToCreate, cancellationToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Unexpected infra error — keep throwing so infrastructure can handle it.
                // We only use Result for expected validation outcomes.
                throw;
            }
        }

        public async Task<Result> UpdateAsync(UpdateEmployeeDto employee, CancellationToken cancellationToken = default)
        {
            Employee employeeToUpdate;
            try
            {
                employeeToUpdate = _employeeFactory.CreateForUpdate(employee);
            }
            catch (ValidationException ex)
            {
                return Result.Failure(ex.Message);
            }

            var employeeRepository = _employeeRepositoryCreator.Create();

            var affectedRows = await employeeRepository.UpdateAsync(employeeToUpdate, cancellationToken);
            if (affectedRows == 0)
            {
                // expected validation-like outcome (not found) — return Result failure
                return Result.Failure("Empleado no encontrado.");
            }

            return Result.Success();
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

            var digitsOnly = new List<char>();
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

            return new string(digitsOnly.ToArray());
        }
    }
}
