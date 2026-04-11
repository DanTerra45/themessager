using Mercadito.src.suppliers.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IGetAllSuppliersUseCase
    {
        Task<Result<IReadOnlyList<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
