using Mercadito.src.categories.domain.entities;
using Mercadito.src.shared.domain.validation;

namespace Mercadito.src.categories.domain.factories
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
