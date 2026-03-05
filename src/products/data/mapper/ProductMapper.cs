using Dapper;
namespace Mercadito
{
    public static class ProductMapper
    {
        public static CreateProductDto ToRegisterProductEntity(RegisterNewProductWithCategoryDto dto)
        {
            return new CreateProductDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Stock = dto.Stock,
                Lote = dto.Lote,
                FechaDeCaducidad = dto.FechaDeCaducidad,
                Price = dto.Price
            };
        }
    }
}