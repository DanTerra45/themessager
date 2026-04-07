using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Shared.Domain;

namespace Mercadito.src.suppliers.application.use_cases
{
    public class DeleteSupplierUseCase : IDeleteSupplierUseCase
    {
        private readonly ISupplierRepository _repository;

        public DeleteSupplierUseCase(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Result<int>.Failure(new Dictionary<string, List<string>> { { "Id", new List<string> { "El ID debe ser válido" } } });
            }

            var rowsAffected = await _repository.DeleteAsync(id, cancellationToken);
            return Result<int>.Success(rowsAffected);
        }
    }
}
