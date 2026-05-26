using ServicioProveedores.Domain.Shared;

namespace ServicioProveedores.Application.Suppliers.Ports.Input
{
    public interface IDeleteSupplierUseCase
    {
        Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default);
    }
}

