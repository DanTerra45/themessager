using Mercadito.src.suppliers.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IGetSupplierByIdUseCase
    {
        Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default);
    }
}
