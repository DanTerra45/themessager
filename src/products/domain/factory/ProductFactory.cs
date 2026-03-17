using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.dto;

namespace Mercadito.src.products.domain.factory
{
    public class ProductFactory : IProductFactory
    {
        public Product CreateForInsert(CreateProductDto dto)
        {
            return new Product
            {
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock,
                Batch = NormalizeBatch(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price
            };
        }

        public Product CreateForUpdate(UpdateProductDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock,
                Batch = NormalizeBatch(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price
            };
        }

        private static string NormalizeRequired(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }

        private static string NormalizeName(string value)
        {
            return CollapseSpaces(NormalizeRequired(value));
        }

        private static string NormalizeBatch(string value)
        {
            return CollapseSpaces(NormalizeRequired(value));
        }

        private static string CollapseSpaces(string value)
        {
            var segments = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', segments);
        }
    }
}
