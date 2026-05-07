using Mercadito.Sales.Api.Application.Suppliers.Models;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Domain.Suppliers.Entities;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.UseCases
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
