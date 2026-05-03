using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.domain.shared.validation;
using Mercadito.src.domain.shared;
using System.ComponentModel.DataAnnotations;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.src.suppliers.application.usecases
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
