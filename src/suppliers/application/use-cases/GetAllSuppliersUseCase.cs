using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.suppliers.domain.entities;
using Shared.Domain;

namespace Mercadito.src.suppliers.application.use_cases
{
    public class GetAllSuppliersUseCase : IGetAllSuppliersUseCase
    {
        private readonly ISupplierRepository _repository;

        public GetAllSuppliersUseCase(ISupplierRepository repository)
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
