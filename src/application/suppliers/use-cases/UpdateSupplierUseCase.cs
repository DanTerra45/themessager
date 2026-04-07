using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.repository;
using Mercadito.src.domain.provedores.validator;
using Mercadito.src.shared.domain.validator;

namespace Mercadito.src.application.suppliers.use_cases
{
    public class UpdateSupplierUseCase
    {
        private readonly SupplierRepository _repository;
        private readonly IValidator<UpdateSupplierDto, SupplierDto> _validator;

        public UpdateSupplierUseCase(SupplierRepository repository, IValidator<UpdateSupplierDto, SupplierDto> validator)
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

            var rowsAffected = await _repository.UpdateAsync(dto, cancellationToken);
            return Result<int>.Success(rowsAffected);
        }
    }
}