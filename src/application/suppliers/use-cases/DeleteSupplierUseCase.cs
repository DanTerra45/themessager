using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.domain.shared;

namespace Mercadito.src.suppliers.application.usecases
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
