using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Shared.Validation;
using ServicioProveedores.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using ServicioProveedores.Domain.Shared.Exceptions;

namespace ServicioProveedores.Application.Suppliers.UseCases
{
    public class RegisterSupplierUseCase(
        ISupplierRepository repository,
        IValidator<CreateSupplierDto, SupplierDto> validator) : IRegisterSupplierUseCase
    {
        public async Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, long actorUserId, CancellationToken cancellationToken = default)
        {
            if (actorUserId <= 0)
            {
                return Result.Failure<long>(new Dictionary<string, List<string>>
                {
                    { "Validation", ["No se pudo resolver el actor de auditoría."] }
                });
            }

            var validationResult = validator.Validate(dto);
            if (validationResult.IsFailure)
            {
                return Result.Failure<long>(validationResult.Errors);
            }

            try
            {
                var supplier = validationResult.Value;
                var normalizedDto = new CreateSupplierDto
                {
                    Codigo = string.Empty,
                    Nombre = supplier.Nombre,
                    Direccion = supplier.Direccion,
                    Contacto = supplier.Contacto,
                    Rubro = supplier.Rubro,
                    Telefono = supplier.Telefono
                };

                var id = await repository.CreateAsync(normalizedDto, actorUserId, cancellationToken);
                return Result.Success(id);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<long>(validationException.Errors);
                }

                return Result.Failure<long>(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure<long>(new Dictionary<string, List<string>>
                {
                    { "Validation", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<long>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure<long>("Se produjo un error inesperado al procesar la solicitud.");
            }
        }
    }
}

