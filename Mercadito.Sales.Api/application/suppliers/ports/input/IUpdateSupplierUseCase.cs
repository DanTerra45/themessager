using Mercadito.src.suppliers.application.models;
using Mercadito.src.domain.shared;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IUpdateSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(UpdateSupplierDto dto, CancellationToken cancellationToken = default);
    }
}
