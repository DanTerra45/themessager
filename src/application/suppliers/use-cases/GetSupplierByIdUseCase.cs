using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.domain.suppliers.entities;
using Mercadito.src.domain.shared;

namespace Mercadito.src.suppliers.application.usecases
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
