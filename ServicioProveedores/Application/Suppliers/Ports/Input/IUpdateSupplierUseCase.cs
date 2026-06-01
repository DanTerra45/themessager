using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Domain.Shared;

namespace ServicioProveedores.Application.Suppliers.Ports.Input
{
    public interface IUpdateSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(UpdateSupplierDto dto, long actorUserId, CancellationToken cancellationToken = default);
    }
}

