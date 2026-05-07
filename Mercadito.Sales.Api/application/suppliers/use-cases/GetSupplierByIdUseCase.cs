using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Domain.Suppliers.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.UseCases
{
    public class GetSupplierByIdUseCase(ISupplierRepository repository) : IGetSupplierByIdUseCase
    {
        public async Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Result.Failure<SupplierDto>(new Dictionary<string, List<string>> { { "Id", new List<string> { "El ID debe ser válido" } } });
            }

            var supplier = await repository.GetByIdAsync(id, cancellationToken);
            if (supplier == null)
            {
                return Result.Failure<SupplierDto>(new Dictionary<string, List<string>> { { "NotFound", new List<string> { "Proveedor no encontrado" } } });
            }

            return Result.Success(MapToDto(supplier));
        }

        private static SupplierDto MapToDto(Supplier supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id.GetValueOrDefault(),
                Codigo = supplier.Codigo,
                Nombre = supplier.RazonSocial,
                Direccion = supplier.Direccion,
                Contacto = supplier.Contacto,
                Rubro = supplier.Rubro,
                Telefono = supplier.Telefono
            };
        }
    }
}
