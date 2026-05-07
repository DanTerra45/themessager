using Mercadito.Sales.Api.Domain.Audit.Entities;
using Mercadito.Sales.Api.Application.Employees.Models;
using Mercadito.Sales.Api.Application.Employees.Ports.Input;
using Mercadito.Sales.Api.Application.Employees.Ports.Output;
using Mercadito.Sales.Api.Application.Employees.Validation;
using Mercadito.Sales.Api.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;

namespace Mercadito.Sales.Api.Application.Employees.UseCases
{
    public class EmployeeManagementUseCase(
        IEmployeeRepository employeeRepository,
        ICreateEmployeeValidator createEmployeeValidator,
        IUpdateEmployeeValidator updateEmployeeValidator) : IEmployeeManagementUseCase
    {
        public async Task<IReadOnlyList<EmployeeModel>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            var employees = await employeeRepository.GetEmployeesByCursorAsync(pageSize, sortBy, sortDirection, cursorEmployeeId, isNextPage, searchTerm, cancellationToken);
            return NormalizeContactsForUi(employees);
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorEmployeeId, string searchTerm, CancellationToken cancellationToken = default)
        {
            var employees = await employeeRepository.GetEmployeesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorEmployeeId, searchTerm, cancellationToken);
            return NormalizeContactsForUi(employees);
        }

        public async Task<bool> HasEmployeesByCursorAsync(string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            return await employeeRepository.HasEmployeesByCursorAsync(sortBy, sortDirection, cursorEmployeeId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<UpdateEmployeeDto?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default)
        {
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
                Cargo = employee.Cargo
            };
        }

        public async Task<Result> CreateAsync(CreateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var validationResult = createEmployeeValidator.Validate(employee);
            if (validationResult.IsFailure)
            {
                if (validationResult.Errors.Count > 0)
                {
                    return Result.Failure(validationResult.Errors);
                }

                return Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                await employeeRepository.CreateAsync(validationResult.Value, cancellationToken);
                return Result.Success();
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure(validationException.Errors);
                }

                return Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<Result> UpdateAsync(UpdateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);

            var validationResult = updateEmployeeValidator.Validate(employee);
            if (validationResult.IsFailure)
            {
                if (validationResult.Errors.Count > 0)
                {
                    return Result.Failure(validationResult.Errors);
                }

                return Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var affectedRows = await employeeRepository.UpdateAsync(validationResult.Value, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure("Empleado no encontrado.");
                }

                return Result.Success();
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure(validationException.Errors);
                }

                return Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<bool> DeleteAsync(long employeeId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);
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
                return new string([.. digitsOnly.GetRange(digitsOnly.Count - 8, 8)]);
            }

            return new string([.. digitsOnly]);
        }

        private static List<EmployeeModel> NormalizeContactsForUi(IReadOnlyList<EmployeeModel> employees)
        {
            var normalizedEmployees = new List<EmployeeModel>(employees.Count);
            foreach (var employee in employees)
            {
                normalizedEmployees.Add(new EmployeeModel
                {
                    Id = employee.Id,
                    Ci = employee.Ci,
                    Complemento = employee.Complemento,
                    Nombres = employee.Nombres,
                    PrimerApellido = employee.PrimerApellido,
                    SegundoApellido = employee.SegundoApellido,
                    Cargo = employee.Cargo,
                    NumeroContacto = NormalizeContactForUi(employee.NumeroContacto)
                });
            }

            return normalizedEmployees;
        }
    }
}
