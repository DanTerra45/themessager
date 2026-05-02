using Mercadito.src.domain.categories.entities;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.domain.categories.factories
{
    public class CategoryFactory : ICategoryFactory
    {
        public Category CreateForInsert(CreateCategoryValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Category
            {
                Code = ValidationText.NormalizeUpperTrimmed(input.Code),
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description)
            };
        }

        public Category CreateForUpdate(UpdateCategoryValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Category
            {
                Id = input.Id,
                Code = ValidationText.NormalizeUpperTrimmed(input.Code),
                Name = ValidationText.NormalizeCollapsed(input.Name),
                Description = ValidationText.NormalizeTrimmed(input.Description)
            };
        }
    }
}
