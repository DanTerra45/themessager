using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Domain.Shared;

namespace ServicioProveedores.Application.Suppliers.Ports.Input
{
    public interface IRegisterSupplierUseCase
    {
        Task<Result<long>> ExecuteAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    }
}

