using Mercadito.src.suppliers.application.models;
using Mercadito.src.shared.domain;

namespace Mercadito.src.suppliers.application.ports.input
{
    public interface IRegisterSupplierUseCase
    {
        Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    }
}
