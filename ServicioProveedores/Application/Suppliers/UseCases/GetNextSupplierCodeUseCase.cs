using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Ports.Output;
using ServicioProveedores.Domain.Shared;
using ServicioProveedores.Domain.Shared.Exceptions;

namespace ServicioProveedores.Application.Suppliers.UseCases
{
    public sealed class GetNextSupplierCodeUseCase(ISupplierRepository repository) : IGetNextSupplierCodeUseCase
    {
        public async Task<Result<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var nextCode = await repository.GetNextSupplierCodeAsync(cancellationToken);
                return Result.Success(nextCode);
            }
            catch (BusinessValidationException validationException)
            {
                if (validationException.Errors.Count > 0)
                {
                    return Result.Failure<string>(validationException.Errors);
                }

                return Result.Failure<string>(new Dictionary<string, List<string>>
                {
                    { "Codigo", [validationException.Message] }
                });
            }
            catch (DataStoreUnavailableException dataStoreException)
            {
                return Result.Failure<string>(dataStoreException.Message);
            }
            catch (Exception)
            {
                return Result.Failure<string>("Se produjo un error inesperado al procesar la solicitud.");
            }
        }
    }
}

