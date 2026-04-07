using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.shared.domain.validator;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.suppliers.application.use_cases
{
    public class UpdateSupplierUseCase : IUpdateSupplierUseCase
    {
        private readonly ISupplierRepository _repository;
        private readonly IValidator<UpdateSupplierDto, SupplierDto> _validator;

        public UpdateSupplierUseCase(ISupplierRepository repository, IValidator<UpdateSupplierDto, SupplierDto> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<Result<int>> ExecuteAsync(UpdateSupplierDto dto, CancellationToken cancellationToken = default)
        {
            var validationResult = _validator.Validate(dto);
            if (validationResult.IsFailure)
            {
                return Result<int>.Failure(validationResult.Errors);
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
                    var currentSupplier = await _repository.GetByIdAsync(normalizedDto.Id, cancellationToken);
                    if (currentSupplier == null)
                    {
                        return Result<int>.Failure("Proveedor no encontrado.");
                    }

                    normalizedDto.Telefono = currentSupplier.Telefono;
                }

                var rowsAffected = await _repository.UpdateAsync(normalizedDto, cancellationToken);
                if (rowsAffected == 0)
                {
                    return Result<int>.Failure("Proveedor no encontrado.");
                }

                return Result<int>.Success(rowsAffected);
            }
            catch (BusinessValidationException validationException)
            {
                return validationException.Errors.Count > 0
                    ? Result<int>.Failure(validationException.Errors)
                    : Result<int>.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result<int>.Failure(validationException.Message);
            }
        }
    }
}
