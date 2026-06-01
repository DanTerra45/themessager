using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Domain.Shared;

namespace ServicioProveedores.Application.Suppliers.Ports.Input
{
    public interface IGetSupplierByIdUseCase
    {
        Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default);
    }
}

