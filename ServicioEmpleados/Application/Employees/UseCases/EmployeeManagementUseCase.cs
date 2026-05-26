using ServicioEmpleados.Domain.Audit.Entities;
using ServicioEmpleados.Application.Employees.Models;
using ServicioEmpleados.Application.Employees.Ports.Input;
using ServicioEmpleados.Application.Employees.Ports.Output;
using ServicioEmpleados.Application.Employees.Validation;
using ServicioEmpleados.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using ServicioEmpleados.Domain.Shared.Exceptions;

namespace ServicioEmpleados.Application.Employees.UseCases
{
    public class EmployeeManagementUseCase(
        IEmployeeRepository employeeRepository,
        ICreateEmployeeValidator createEmployeeValidator,
        IUpdateEmployeeValidator updateEmployeeValidator) : IEmployeeManagementUseCase
    {
        public async Task<Result<IReadOnlyList<EmployeeModel>>> GetPageByCursorAsync(int pageSize, string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var employees = await employeeRepository.GetEmployeesByCursorAsync(pageSize, sortBy, sortDirection, cursorEmployeeId, isNextPage, searchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<EmployeeModel>>(NormalizeContactsForUi(employees));
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<EmployeeModel>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<IReadOnlyList<EmployeeModel>>();
            }
        }

        public async Task<Result<IReadOnlyList<EmployeeModel>>> GetPageFromAnchorAsync(int pageSize, string sortBy, string sortDirection, long anchorEmployeeId, string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var employees = await employeeRepository.GetEmployeesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorEmployeeId, searchTerm, cancellationToken);
                return Result.Success<IReadOnlyList<EmployeeModel>>(NormalizeContactsForUi(employees));
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<EmployeeModel>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<IReadOnlyList<EmployeeModel>>();
            }
        }

        public async Task<Result<bool>> HasEmployeesByCursorAsync(string sortBy, string sortDirection, long cursorEmployeeId, bool isNextPage, string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                var hasRows = await employeeRepository.HasEmployeesByCursorAsync(sortBy, sortDirection, cursorEmployeeId, isNextPage, searchTerm, cancellationToken);
                return Result.Success(hasRows);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<bool>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<bool>();
            }
        }

        public async Task<Result<UpdateEmployeeDto>> GetForEditAsync(long employeeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
                if (employee == null)
                {
                    return Result.Failure<UpdateEmployeeDto>(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["Empleado no encontrado."] }
                    });
                }

                return Result.Success(new UpdateEmployeeDto
                {
                    Id = employee.Id,
                    Ci = employee.Ci,
                    Complemento = employee.Complemento,
                    Nombres = employee.Nombres,
                    PrimerApellido = employee.PrimerApellido,
                    SegundoApellido = employee.SegundoApellido,
                    NumeroContacto = NormalizeContactForUi(employee.NumeroContacto),
                    Cargo = employee.Cargo
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<UpdateEmployeeDto>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure<UpdateEmployeeDto>();
            }
        }

        public async Task<Result> CreateAsync(CreateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (actor == null)
            {
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
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
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure();
            }
        }

        public async Task<Result> UpdateAsync(UpdateEmployeeDto employee, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (actor == null)
            {
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
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
                var affectedRows = await employeeRepository.UpdateAsync(validationResult.Value, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["Empleado no encontrado."] }
                    });
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
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure();
            }
        }

        public async Task<Result> DeleteAsync(long employeeId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (actor == null)
            {
                return Result.Failure(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
            }

            try
            {
                var affectedRows = await employeeRepository.DeleteAsync(employeeId, cancellationToken);
                if (affectedRows == 0)
                {
                    return Result.Failure(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["El empleado no existe o ya estaba desactivado."] }
                    });
                }

                return Result.Success();
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure(dataStoreException.Message);
            }
            catch (Exception)
            {
                return UnexpectedFailure();
            }
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

        private static Result UnexpectedFailure()
        {
            return Result.Failure("Se produjo un error inesperado al procesar la solicitud.");
        }

        private static Result<T> UnexpectedFailure<T>()
        {
            return Result.Failure<T>("Se produjo un error inesperado al procesar la solicitud.");
        }
    }
}

