namespace MSProducto.Application.Services
{
    using Microsoft.Extensions.Logging;
    using MSProducto.Domain.Entities;
    using MSProducto.Domain.Ports.Input;
    using MSProducto.Domain.Ports.Output;
    using MSProducto.Domain.Common;

    public class CategoriaService : ICategoriaService
    {
        private readonly ICategoriaRepository _repository;
        private readonly ICookieUserExtractor _userExtractor;
        private readonly ILogger<CategoriaService> _logger;

        public CategoriaService(
            ICategoriaRepository repository,
            ICookieUserExtractor userExtractor,
            ILogger<CategoriaService> logger)
        {
            _repository = repository;
            _userExtractor = userExtractor;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Categoria>> GetPageByCursorAsync(
            int pageSize, string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetCategoriesByCursorAsync(pageSize, sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<IReadOnlyList<Categoria>> GetPageFromAnchorAsync(
            int pageSize, string sortBy, string sortDirection, long anchorCategoryId, string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return await _repository.GetCategoriesFromAnchorAsync(pageSize, sortBy, sortDirection, anchorCategoryId, searchTerm, cancellationToken);
        }

        public async Task<bool> HasCategoriesByCursorAsync(
            string sortBy, string sortDirection, long cursorCategoryId, bool isNextPage, string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return await _repository.HasCategoriesByCursorAsync(sortBy, sortDirection, cursorCategoryId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<string> GetNextCategoryCodePreviewAsync(CancellationToken cancellationToken = default)
        {
            return await _repository.GetNextCategoryCodeAsync(cancellationToken);
        }

        public async Task<Categoria?> GetForEditAsync(long categoryId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByIdAsync(categoryId, cancellationToken);
        }

        public async Task<Result<long>> CreateAsync(Categoria newCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (!_userExtractor.IsAuthenticated())
                return Result.Failure<long>("Acceso denegado: usuario no autenticado.");

            _logger.LogInformation("Action: Create by {U}", _userExtractor.GetCurrentUsername());

            var createdId = await _repository.CreateAsync(newCategory, cancellationToken);
            return Result.Success(createdId);
        }

        public async Task<Result> UpdateAsync(Categoria editCategory, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (!_userExtractor.IsAuthenticated())
                return Result.Failure("Acceso denegado: usuario no autenticado.");

            _logger.LogInformation("Action: Update by {U}", _userExtractor.GetCurrentUsername());

            var affectedRows = await _repository.UpdateAsync(editCategory, cancellationToken);
            if (affectedRows == 0)
                return Result.Failure("Categoría no encontrada.");

            return Result.Success();
        }

        public async Task<bool> DeleteAsync(long categoryId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (!_userExtractor.IsAuthenticated())
                return false;

            _logger.LogInformation("Action: Delete by {U}", _userExtractor.GetCurrentUsername());

            var affectedRows = await _repository.DeleteAsync(categoryId, cancellationToken);
            return affectedRows > 0;
        }
    }
}