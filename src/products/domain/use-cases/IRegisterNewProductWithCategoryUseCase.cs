using Mercadito.src.products.domain.dto;

namespace Mercadito.src.products.domain.usecases
{
    public interface IRegisterNewProductWithCategoryUseCase
    {
        Task ExecuteAsync(CreateProductDto newProduct, CancellationToken cancellationToken = default);
    }
}