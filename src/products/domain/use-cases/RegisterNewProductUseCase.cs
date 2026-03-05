using System;

namespace Mercadito;

public class RegisterNewProductUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<RegisterNewProductUseCase> _logger;

    public RegisterNewProductUseCase(IProductRepository productRepository, ICategoryRepository categoryRepository, ILogger<RegisterNewProductUseCase> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<long> ExecuteAsync(CreateProductDto product)
    {
        try
        {
            return await _productRepository.AddProductAsync(product);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error al registrar el nuevo producto: {Message}", ex.Message);
            throw;
        }
    }
}
