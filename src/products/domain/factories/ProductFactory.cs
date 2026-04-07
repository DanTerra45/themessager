using Mercadito.src.products.domain.entities;
using Mercadito.src.products.application.models;
using System;

namespace Mercadito.src.products.domain.factories
{
    public class ProductFactory : IProductFactory
    {
        public Product CreateForInsert(CreateProductDto dto)
        {
            return new Product
            {
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock.GetValueOrDefault(0),
                Batch = NormalizeBatch(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price.GetValueOrDefault(0m),
                CategoryIds = NormalizeCategoryIds(dto.CategoryIds)
            };
        }

        public Product CreateForUpdate(UpdateProductDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock.GetValueOrDefault(0),
                Batch = NormalizeBatch(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price.GetValueOrDefault(0m),
                CategoryIds = NormalizeCategoryIds(dto.CategoryIds)
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
            return NormalizeRequired(value);
        }

        private static IReadOnlyList<long> NormalizeCategoryIds(IEnumerable<long> categoryIds)
        {
            var normalizedCategoryIds = new List<long>();
            var distinctCategoryIds = new HashSet<long>();

            foreach (var categoryId in categoryIds)
            {
                if (categoryId > 0 && distinctCategoryIds.Add(categoryId))
                {
                    normalizedCategoryIds.Add(categoryId);
                }
            }

            return normalizedCategoryIds;
        }

        private static string CollapseSpaces(string value)
        {
            var segments = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', segments);
        }
    }
}
