using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.suppliers.domain.entities;
using Mercadito.src.shared.domain;

namespace Mercadito.src.suppliers.application.usecases
{
    public class GetAllSuppliersUseCase(ISupplierRepository repository) : IGetAllSuppliersUseCase
    {
        public async Task<Result<IReadOnlyList<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var suppliers = await repository.GetAllAsync(cancellationToken);
            var dtos = suppliers.Select(MapToDto).ToList();
            return Result.Success<IReadOnlyList<SupplierDto>>(dtos);
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
