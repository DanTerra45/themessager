using ServicioProveedores.Domain.Shared;

namespace ServicioProveedores.Application.Suppliers.Ports.Input
{
    public interface IGetNextSupplierCodeUseCase
    {
        Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}

