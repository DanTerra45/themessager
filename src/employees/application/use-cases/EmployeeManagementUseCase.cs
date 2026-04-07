using Mercadito.src.audit.application.services;
using Mercadito.src.audit.domain.entities;
using Mercadito.src.employees.application.models;
using Mercadito.src.employees.application.ports.input;
using Mercadito.src.employees.application.ports.output;
using Mercadito.src.employees.application.validation;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.employees.application.use_cases
{
    public class EmployeeManagementUseCase : IEmployeeManagementUseCase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICreateEmployeeValidator _createEmployeeValidator;
        private readonly IUpdateEmployeeValidator _updateEmployeeValidator;
        private readonly IAuditTrailService _auditTrailService;

        public EmployeeManagementUseCase(
            IEmployeeRepository employeeRepository,
            ICreateEmployeeValidator createEmployeeValidator,
            IUpdateEmployeeValidator updateEmployeeValidator,
            IAuditTrailService auditTrailService)
        {
            _employeeRepository = employeeRepository;
            _createEmployeeValidator = createEmployeeValidator;
            _updateEmployeeValidator = updateEmployeeValidator;
            _auditTrailService = auditTrailService;
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, CancellationToken cancellationToken = default)
        {
            return await _employeeRepository.GetEmployeesByCursorAsync(pageSize, sortBy, sortDirection, cursorEmployeeId, isNextPage, cancellationToken);
        }

        public async Task<IReadOnlyList<EmployeeModel>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorEmployeeId, CancellationToken cancellationToken = default)
        {
            return await _employeeRepository.GetEmployeesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorEmployeeId, cancellationToken);
        }

        public async Task<bool> HasEmployeesByCursorAsync(string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, CancellationToken cancellationToken = default)
        {
            return await _employeeRepository.HasEmployeesByCursorAsync(sortBy, sortDirection, cursorEmployeeId, isNextPage, cancellationToken);
        }

        public async Task<UpdateEmployeeDto?> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
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
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = _createEmployeeValidator.Validate(employee);
            if (validationResult.IsFailure)
            {
                return validationResult.Errors.Count > 0
                    ? Result.Failure(validationResult.Errors)
                    : Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var employeeId = await _employeeRepository.CreateAsync(validationResult.Value, cancellationToken);
                await _auditTrailService.RecordAsync(
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
                return validationException.Errors.Count > 0
                    ? Result.Failure(validationException.Errors)
                    : Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<Result> UpdateAsync(UpdateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return actorValidation;
            }

            var validationResult = _updateEmployeeValidator.Validate(employee);
            if (validationResult.IsFailure)
            {
                return validationResult.Errors.Count > 0
                    ? Result.Failure(validationResult.Errors)
                    : Result.Failure(validationResult.ErrorMessage);
            }

            try
            {
                var previousEmployee = await _employeeRepository.GetByIdAsync(validationResult.Value.Id, cancellationToken);
                var affectedRows = await _employeeRepository.UpdateAsync(validationResult.Value, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure("Empleado no encontrado.");
                }

                await _auditTrailService.RecordAsync(
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
                return validationException.Errors.Count > 0
                    ? Result.Failure(validationException.Errors)
                    : Result.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure(validationException.Message);
            }
        }

        public async Task<bool> DeleteAsync(long employeeId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            var actorValidation = _auditTrailService.ValidateActor(actor);
            if (actorValidation.IsFailure)
            {
                return false;
            }

            var previousEmployee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            var affectedRows = await _employeeRepository.DeleteAsync(employeeId, cancellationToken);
            if (affectedRows > 0 && previousEmployee != null)
            {
                await _auditTrailService.RecordAsync(
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
                return new string(digitsOnly.GetRange(digitsOnly.Count - 8, 8).ToArray());
            }

            return new string(digitsOnly.ToArray());
        }
    }
}
