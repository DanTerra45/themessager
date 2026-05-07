using Mercadito.Sales.Api.Application.Suppliers.Ports.Input;
using Mercadito.Sales.Api.Application.Suppliers.Ports.Output;
using Mercadito.Sales.Api.Domain.Shared;

namespace Mercadito.Sales.Api.Application.Suppliers.UseCases
{
    public class DeleteSupplierUseCase(ISupplierRepository repository) : IDeleteSupplierUseCase
    {
        public async Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Result.Failure<int>(new Dictionary<string, List<string>> { { "Id", new List<string> { "El ID debe ser válido" } } });
            }

            var rowsAffected = await repository.DeleteAsync(id, cancellationToken);
            return Result.Success(rowsAffected);
        }
    }
}
