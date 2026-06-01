using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Domain.Shared;

namespace ServicioProveedores.Application.Suppliers.Ports.Input
{
    public interface IGetAllSuppliersUseCase
    {
        Task<Result<IReadOnlyList<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}

