using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Domain.Suppliers.Entities;

namespace Mercadito.Sales.Api.Application.Suppliers.Ports.Output
{
    public interface ISupplierRepository
    {
        Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default);
        Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<long> CreateAsync(CreateSupplierDto entity, CancellationToken cancellationToken = default);
        Task<int> UpdateAsync(UpdateSupplierDto entity, CancellationToken cancellationToken = default);
        Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
