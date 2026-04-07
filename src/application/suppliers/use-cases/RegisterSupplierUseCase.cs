using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.repository;
using Mercadito.src.domain.provedores.validator;
using Mercadito.src.shared.domain.validator;

namespace Mercadito.src.application.suppliers.use_cases
{
    public class RegisterSupplierUseCase : IRegisterSupplierUseCase
    {
        private readonly SupplierRepository _repository;
        private readonly IValidator<CreateSupplierDto, SupplierDto> _validator;

        public RegisterSupplierUseCase(SupplierRepository repository, IValidator<CreateSupplierDto, SupplierDto> validator)
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

            var id = await _repository.CreateAsync(dto, cancellationToken);
            return Result<long>.Success(id);
        }
    }
}