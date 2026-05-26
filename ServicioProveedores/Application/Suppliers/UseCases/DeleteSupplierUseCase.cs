using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Shared;
using ServicioProveedores.Domain.Shared.Exceptions;

namespace ServicioProveedores.Application.Suppliers.UseCases
{
    public class DeleteSupplierUseCase(ISupplierRepository repository) : IDeleteSupplierUseCase
    {
        public async Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    return Result.Failure<int>(new Dictionary<string, List<string>> { { "Id", new List<string> { "El ID debe ser válido" } } });
                }

                var rowsAffected = await repository.DeleteAsync(id, cancellationToken);
                if (rowsAffected == 0)
                {
                    return Result.Failure<int>(new Dictionary<string, List<string>>
                    {
                        { "NotFound", ["El proveedor no existe o ya estaba desactivado."] }
                    });
                }

                return Result.Success(rowsAffected);
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<int>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure<int>("Se produjo un error inesperado al procesar la solicitud.");
            }
        }
    }
}
