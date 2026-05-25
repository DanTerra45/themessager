namespace Mercadito.Frontend.Services;

using Mercadito.Frontend.Dtos.Products;

public interface IProductoApiClient
{
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(long id);
    Task<(bool Success, string? Error)> CreateProductAsync(SaveProductRequestDto request);
    Task<(bool Success, string? Error)> UpdateProductAsync(long id, SaveProductRequestDto request);
    Task<(bool Success, string? Error)> DeleteProductAsync(long id);
}