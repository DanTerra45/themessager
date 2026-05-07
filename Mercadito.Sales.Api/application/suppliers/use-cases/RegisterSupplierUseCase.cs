using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Domain.Shared.Validation;
using Mercadito.Sales.Api.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using Mercadito.Sales.Api.Domain.Shared.Exceptions;

namespace Mercadito.Sales.Api.Application.Suppliers.UseCases
{
    public class RegisterSupplierUseCase(
        ISupplierRepository repository,
        IValidator<CreateSupplierDto, SupplierDto> validator) : IRegisterSupplierUseCase
    {
        public async Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
        {
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

                var id = await repository.CreateAsync(normalizedDto, cancellationToken);
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
                return Result.Failure<long>(validationException.Message);
            }
        }
    }
}
