using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Domain.Suppliers.Entities;

namespace ServicioProveedores.Application.Suppliers.Ports.Output
{
    public interface ISupplierRepository
    {
        Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default);
        Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(CreateSupplierDto entity, long actorUserId, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(UpdateSupplierDto entity, long actorUserId, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, long actorUserId, CancellationToken cancellationToken = default);
    }
}

