using Mercadito.src.suppliers.application.models;
using Shared.Domain;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IGetAllSuppliersUseCase
    {
        Task<Result<List<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
