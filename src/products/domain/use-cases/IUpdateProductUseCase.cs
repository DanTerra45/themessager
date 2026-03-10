using Mercadito.src.products.domain.dto;

namespace Mercadito.src.products.domain.usecases
{
    public interface IUpdateProductUseCase
    {
        Task ExecuteAsync(UpdateProductDto updateProduct);
    }
}