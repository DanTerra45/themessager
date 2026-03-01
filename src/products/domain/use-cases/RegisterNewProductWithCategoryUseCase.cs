using Dapper;

namespace Mercadito
{
    public class RegisterNewProductWithCategoryUseCase
    {
        
        private readonly RegisterNewProductUseCase _registerProductCase;
        private readonly AsignCategoryToProductUseCase _asignCategoryCase;
        public RegisterNewProductWithCategoryUseCase(RegisterNewProductUseCase registerProductCase, AsignCategoryToProductUseCase asignCategoryCase)
        {
            _registerProductCase = registerProductCase;
            _asignCategoryCase = asignCategoryCase;
        }
        public async Task ExecuteAsync(RegisterNewProductWithCategoryDto product)
        {
            try
            {
                var productId = await _registerProductCase.ExecuteAsync(ProductMapper.ToRegisterProductEntity(product));
                await _asignCategoryCase.ExecuteAsync(productId, product.CategoryId);
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException("Error al registrar producto con categoría", ex);
            }
        }
            
    }
}