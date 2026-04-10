using Mercadito.src.products.domain.entities;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.products.domain.factories
{
    public class ProductFactory : IProductFactory
    {
        public Product CreateForInsert(CreateProductValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Product
            {
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description),
                Stock = input.Stock.GetValueOrDefault(0),
                Batch = ValidationText.NormalizeTrimmed(input.Batch),
                ExpirationDate = input.ExpirationDate,
                Price = input.Price.GetValueOrDefault(0m),
                CategoryIds = NormalizeCategoryIds(input.CategoryIds)
            };
        }

        public Product CreateForUpdate(UpdateProductValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Product
            {
                Id = input.Id,
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description),
                Stock = input.Stock.GetValueOrDefault(0),
                Batch = ValidationText.NormalizeTrimmed(input.Batch),
                ExpirationDate = input.ExpirationDate,
                Price = input.Price.GetValueOrDefault(0m),
                CategoryIds = NormalizeCategoryIds(input.CategoryIds)
            };
        }

        private static List<long> NormalizeCategoryIds(IEnumerable<long> categoryIds)
        {
            ArgumentNullException.ThrowIfNull(categoryIds);

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

    }
}
