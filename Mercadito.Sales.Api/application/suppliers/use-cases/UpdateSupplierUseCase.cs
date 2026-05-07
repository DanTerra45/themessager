using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Domain.Shared.Validation;
using Mercadito.Sales.Api.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;

namespace Mercadito.Sales.Api.Application.Suppliers.UseCases
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
                        return Result.Failure<int>("Proveedor no encontrado.");
                    }

                    normalizedDto.Telefono = currentSupplier.Telefono;
                }

                var rowsAffected = await repository.UpdateAsync(normalizedDto, cancellationToken);
                if (rowsAffected == 0)
                {
                    return Result.Failure<int>("Proveedor no encontrado.");
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
                return Result.Failure<int>(validationException.Message);
            }
        }
    }
}
