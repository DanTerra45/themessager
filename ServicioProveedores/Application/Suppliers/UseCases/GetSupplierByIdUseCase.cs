using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Suppliers.Entities;
using ServicioProveedores.Domain.Shared;
using ServicioProveedores.Domain.Shared.Exceptions;

namespace ServicioProveedores.Application.Suppliers.UseCases
{
    public class GetSupplierByIdUseCase(ISupplierRepository repository) : IGetSupplierByIdUseCase
    {
        public async Task<Result<SupplierDto>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            try
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
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<SupplierDto>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure<SupplierDto>("Se produjo un error inesperado al procesar la solicitud.");
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

