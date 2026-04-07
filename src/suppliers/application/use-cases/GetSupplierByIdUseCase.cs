using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.suppliers.domain.entities;
using Shared.Domain;

namespace Mercadito.src.suppliers.application.use_cases
{
    public class GetSupplierByIdUseCase : IGetSupplierByIdUseCase
    {
        private readonly ISupplierRepository _repository;

        public GetSupplierByIdUseCase(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Result<SupplierDto>.Failure(new Dictionary<string, List<string>> { { "Id", new List<string> { "El ID debe ser válido" } } });
            }

            var supplier = await _repository.GetByIdAsync(id, cancellationToken);
            if (supplier == null)
            {
                return Result<SupplierDto>.Failure(new Dictionary<string, List<string>> { { "NotFound", new List<string> { "Proveedor no encontrado" } } });
            }

            return Result<SupplierDto>.Success(MapToDto(supplier));
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
