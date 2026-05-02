using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Products;

namespace Mercadito.Frontend.Adapters.Products;

public interface IProductsApiAdapter
{
    Task<ApiResponseDto<ProductPageDto>> GetProductsAsync(
        long categoryFilter,
        int pageSize,
        string sortBy,
        string sortDirection,
        long anchorProductId,
        long cursorProductId,
        bool isNextPage,
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<ProductForEditDto>> GetProductAsync(
        long productId,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CreateProductAsync(
        SaveProductRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> UpdateProductAsync(
        long productId,
        SaveProductRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> DeleteProductAsync(
        long productId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);
}
