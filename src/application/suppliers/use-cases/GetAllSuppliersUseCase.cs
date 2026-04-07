using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.model;
using Mercadito.src.domain.provedores.repository;
using Mercadito.src.shared.domain.validator;

namespace Mercadito.src.application.suppliers.use_cases
{
    public class GetAllSuppliersUseCase
    {
        private readonly SupplierRepository _repository;

        public GetAllSuppliersUseCase(SupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<List<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var suppliers = await _repository.GetAllAsync(cancellationToken);
            
            var dtos = suppliers.Select(MapToDto).ToList();
            return Result<List<SupplierDto>>.Success(dtos);
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