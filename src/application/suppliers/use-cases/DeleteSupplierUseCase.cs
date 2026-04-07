using Mercadito.src.domain.provedores.repository;
using Mercadito.src.shared.domain.validator;

namespace Mercadito.src.application.suppliers.use_cases
{
    public class DeleteSupplierUseCase
    {
        private readonly SupplierRepository _repository;

        public DeleteSupplierUseCase(SupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<int>> ExecuteAsync(long id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Result<int>.Failure(new Dictionary<string, List<string>>
                {
                    { "Id", new List<string> { "El ID debe ser válido" } }
                });
            }

            var rowsAffected = await _repository.DeleteAsync(id, cancellationToken);
            return Result<int>.Success(rowsAffected);
        }
    }
}