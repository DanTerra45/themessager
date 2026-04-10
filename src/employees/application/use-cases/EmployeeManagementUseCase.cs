using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.employees.application.models;
using Mercadito.src.employees.application.ports.input;
using Mercadito.src.employees.application.ports.output;
using Mercadito.src.employees.application.validation;
using Mercadito.src.shared.domain;
using System.ComponentModel.DataAnnotations;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.employees.application.usecases
{
    public class EmployeeManagementUseCase(
        IEmployeeRepository employeeRepository,
        ICreateEmployeeValidator createEmployeeValidator,
        IUpdateEmployeeValidator updateEmployeeValidator,
        IAuditTrailService auditTrailService) : IEmployeeManagementUseCase
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
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

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
                var employeeId = await employeeRepository.CreateAsync(validationResult.Value, cancellationToken);
                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Create,
                    "empleados",
                    employeeId,
                    null,
                    validationResult.Value,
                    cancellationToken);

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
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

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
                var previousEmployee = await employeeRepository.GetByIdAsync(validationResult.Value.Id, cancellationToken);
                var affectedRows = await employeeRepository.UpdateAsync(validationResult.Value, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure("Empleado no encontrado.");
                }

                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Update,
                    "empleados",
                    validationResult.Value.Id,
                    previousEmployee,
                    validationResult.Value,
                    cancellationToken);

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
            var actorValidation = auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return false;
            }

            var previousEmployee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            var affectedRows = await employeeRepository.DeleteAsync(employeeId, cancellationToken);
            if (affectedRows > 0 && previousEmployee != null)
            {
                await auditTrailService.RecordAsync(
                    actor,
                    AuditAction.Delete,
                    "empleados",
                    employeeId,
                    previousEmployee,
                    new { Estado = "I" },
                    cancellationToken);
            }

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
