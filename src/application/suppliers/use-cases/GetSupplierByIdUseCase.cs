using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.model;
using Mercadito.src.domain.provedores.repository;
using Mercadito.src.shared.domain.validator;

namespace Mercadito.src.application.suppliers.use_cases
{
    public class GetSupplierByIdUseCase
    {
        private readonly SupplierRepository _repository;

        public GetSupplierByIdUseCase(SupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Result<SupplierDto>.Failure(new Dictionary<string, List<string>>
                {
                    { "Id", new List<string> { "El ID debe ser válido" } }
                });
            }

            var supplier = await _repository.GetByIdAsync(id, cancellationToken);
            
            if (supplier == null)
            {
                return Result<SupplierDto>.Failure(new Dictionary<string, List<string>>
                {
                    { "NotFound", new List<string> { "Proveedor no encontrado" } }
                });
            }

            var dto = MapToDto(supplier);
            return Result<SupplierDto>.Success(dto);
        }

        private static SupplierDto MapToDto(Supplier supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id ?? 0,
                Codigo = supplier.Codigo,
                Nombre = supplier.RazonSocial,
                Direccion = supplier.Direccion,
                Contacto = supplier.Contacto,
                Rubro = supplier.Rubro
            };
        }
    }
}