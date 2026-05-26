using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Shared.Validation;
using ServicioProveedores.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using ServicioProveedores.Domain.Shared.Exceptions;

namespace ServicioProveedores.Application.Suppliers.UseCases
{
    public class UpdateSupplierUseCase(
        ISupplierRepository repository,
        IValidator<UpdateSupplierDto, SupplierDto> validator) : IUpdateSupplierUseCase
    {
        public async Task<Result<int>> ExecuteAsync(UpdateSupplierDto dto, CancellationToken cancellationToken = default)
        {
            var validationResult = validator.Validate(dto);
            if (validationResult.IsFailure)
            {
                return Result.Failure<int>(validationResult.Errors);
            }

            try
            {
                var supplier = validationResult.Value;
                var normalizedDto = new UpdateSupplierDto
                {
                    Id = supplier.Id,
                    Codigo = supplier.Codigo,
                    Nombre = supplier.Nombre,
                    Direccion = supplier.Direccion,
                    Contacto = supplier.Contacto,
                    Rubro = supplier.Rubro,
                    Telefono = supplier.Telefono
                };

                if (string.IsNullOrWhiteSpace(normalizedDto.Telefono))
                {
                    var currentSupplier = await repository.GetByIdAsync(normalizedDto.Id, cancellationToken);
                    if (currentSupplier == null)
                    {
                        return Result.Failure<int>(new Dictionary<string, List<string>>
                        {
                            { "NotFound", ["Proveedor no encontrado."] }
                        });
                    }

                    normalizedDto.Telefono = currentSupplier.Telefono;
                }

                var rowsAffected = await repository.UpdateAsync(normalizedDto, cancellationToken);
                if (rowsAffected == 0)
                {
                    return Result.Failure<int>(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["Proveedor no encontrado."] }
                    });
                }

                return Result.Success(rowsAffected);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<int>(validationException.Errors);
                }

                return Result.Failure<int>(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result.Failure<int>(new Dictionary<string, List<string>>
                {
                    { "Validation", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<int>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure<int>("Se produjo un error inesperado al procesar la solicitud.");
            }
        }
    }
}

