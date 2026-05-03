using Mercadito.src.domain.shared;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IDeleteSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default);
    }
}
