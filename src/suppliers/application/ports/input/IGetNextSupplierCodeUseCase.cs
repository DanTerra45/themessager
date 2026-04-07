using Shared.Domain;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IGetNextSupplierCodeUseCase
    {
        Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
