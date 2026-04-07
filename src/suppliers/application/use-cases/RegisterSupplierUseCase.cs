using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.shared.domain.validator;
using Shared.Domain;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.suppliers.application.use_cases
{
    public class RegisterSupplierUseCase : IRegisterSupplierUseCase
    {
        private readonly ISupplierRepository _repository;
        private readonly IValidator<CreateSupplierDto, SupplierDto> _validator;

        public RegisterSupplierUseCase(ISupplierRepository repository, IValidator<CreateSupplierDto, SupplierDto> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
        {
            var validationResult = _validator.Validate(dto);
            if (validationResult.IsFailure)
            {
                return Result<long>.Failure(validationResult.Errors);
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

                var id = await _repository.CreateAsync(normalizedDto, cancellationToken);
                return Result<long>.Success(id);
            }
            catch (BusinessValidationException validationException)
            {
                return validationException.Errors.Count > 0
                    ? Result<long>.Failure(validationException.Errors)
                    : Result<long>.Failure(validationException.Message);
            }
            catch (ValidationException validationException)
            {
                return Result<long>.Failure(validationException.Message);
            }
        }
    }
}
