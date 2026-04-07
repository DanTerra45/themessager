using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Shared.Domain;

namespace Mercadito.src.suppliers.application.use_cases
{
    public sealed class GetNextSupplierCodeUseCase : IGetNextSupplierCodeUseCase
    {
        private readonly ISupplierRepository _repository;

        public GetNextSupplierCodeUseCase(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var nextCode = await _repository.GetNextSupplierCodeAsync(cancellationToken);
                return Result<string>.Success(nextCode);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result<string>.Failure(validationException.Errors);
                }

                return Result<string>.Failure(new Dictionary<string, List<string>>
                {
                    { "Codigo", [validationException.Message] }
                });
            }
        }
    }
}
