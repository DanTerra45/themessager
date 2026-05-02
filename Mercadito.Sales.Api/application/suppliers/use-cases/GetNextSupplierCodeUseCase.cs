using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.domain.shared;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.src.suppliers.application.usecases
{
    public sealed class GetNextSupplierCodeUseCase(ISupplierRepository repository) : IGetNextSupplierCodeUseCase
    {
        public async Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var nextCode = await repository.GetNextSupplierCodeAsync(cancellationToken);
                return Result.Success(nextCode);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<string>(validationException.Errors);
                }

                return Result.Failure<string>(new Dictionary<string, List<string>>
                {
                    { "Codigo", [validationException.Message] }
                });
            }
        }
    }
}
