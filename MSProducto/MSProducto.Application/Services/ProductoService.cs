namespace MSProducto.Application.Services
{
    using Microsoft.Extensions.Logging;
    using MSProducto.Domain.Entities;
    using MSProducto.Domain.Ports.Input;
    using MSProducto.Domain.Ports.Output;
    using MSProducto.Domain.Common;

    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly ICookieUserExtractor _userExtractor;
        private readonly ILogger<ProductoService> _logger;

        public ProductoService(
            IProductoRepository productoRepository,
            ICategoriaRepository categoriaRepository,
            ICookieUserExtractor userExtractor,
            ILogger<ProductoService> logger)
        {
            _productoRepository = productoRepository;
            _categoriaRepository = categoriaRepository;
            _userExtractor = userExtractor;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Categoria>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await _categoriaRepository.GetAllCategoriesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Producto>> GetPageByCursorAsync(
            long categoryFilter, int pageSize, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            if (cursorProductId <= 0)
            {
                return await _productoRepository.GetProductsWithCategoriesFromAnchorAsync(
                    categoryFilter, pageSize, sortBy, sortDirection, 0, searchTerm, cancellationToken);
            }

            if (categoryFilter == 0)
            {
                return await _productoRepository.GetProductsWithCategoriesByCursorAsync(
                    pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
            }

            return await _productoRepository.GetProductsWithCategoriesByCategoryCursorAsync(
                categoryFilter, pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<IReadOnlyList<Producto>> GetPageFromAnchorAsync(
            long categoryFilter, int pageSize, string sortBy, string sortDirection, long anchorProductId, string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            return await _productoRepository.GetProductsWithCategoriesFromAnchorAsync(
                categoryFilter, pageSize, sortBy, sortDirection, anchorProductId, searchTerm, cancellationToken);
        }

        public async Task<bool> HasProductsByCursorAsync(
            long categoryFilter, string sortBy, string sortDirection, long cursorProductId, bool isNextPage, string searchTerm = "",
            CancellationToken cancellationToken = default)
        {
            return await _productoRepository.HasProductsByCursorAsync(
                categoryFilter, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, cancellationToken);
        }

        public async Task<Producto?> GetForEditAsync(long productId, CancellationToken cancellationToken = default)
        {
            return await _productoRepository.GetByIdAsync(productId, cancellationToken);
        }

        public async Task<bool> DeleteAsync(long productId, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (!_userExtractor.IsAuthenticated())
                return false;

            _logger.LogInformation("Action: Delete by {U}", _userExtractor.GetCurrentUsername());

            var affectedRows = await _productoRepository.DeleteAsync(productId, cancellationToken);
            return affectedRows > 0;
        }

        public async Task<Result<long>> CreateAsync(Producto newProduct, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (!_userExtractor.IsAuthenticated())
                return Result.Failure<long>("Acceso denegado: usuario no autenticado.");

            _logger.LogInformation("Action: Create by {U}", _userExtractor.GetCurrentUsername());

            var createdId = await _productoRepository.CreateAsync(newProduct, cancellationToken);
            if (createdId > 0)
            {
                return Result.Success(createdId);
            }

            return Result.Failure<long>("Failed to create product.");
        }

        public async Task<Result> UpdateAsync(Producto updateProduct, AuditActor actor, CancellationToken cancellationToken = default)
        {
            if (!_userExtractor.IsAuthenticated())
                return Result.Failure("Acceso denegado: usuario no autenticado.");

            _logger.LogInformation("Action: Update by {U}", _userExtractor.GetCurrentUsername());

            var affectedRows = await _productoRepository.UpdateAsync(updateProduct, cancellationToken);
            if (affectedRows > 0)
            {
                return Result.Success();
            }

            return Result.Failure("Failed to update product.");
        }
    }
}