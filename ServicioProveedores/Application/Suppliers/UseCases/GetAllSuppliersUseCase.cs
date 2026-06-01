using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Suppliers.Entities;
using ServicioProveedores.Domain.Shared;
using ServicioProveedores.Domain.Shared.Exceptions;

namespace ServicioProveedores.Application.Suppliers.UseCases
{
    public class GetAllSuppliersUseCase(ISupplierRepository repository) : IGetAllSuppliersUseCase
    {
        public async Task<Result<IReadOnlyList<SupplierDto>>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var suppliers = await repository.GetAllAsync(cancellationToken);
                var dtos = suppliers.Select(MapToDto).ToList();
                return Result.Success<IReadOnlyList<SupplierDto>>(dtos);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<IReadOnlyList<SupplierDto>>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure<IReadOnlyList<SupplierDto>>("Se produjo un error inesperado al procesar la solicitud.");
            }
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

