using Mercadito.src.domain.shared;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IGetNextSupplierCodeUseCase
    {
        Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
