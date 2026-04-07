using Mercadito.src.suppliers.application.models;
using Shared.Domain;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IUpdateSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(UpdateSupplierDto dto, CancellationToken cancellationToken = default);
    }
}
