using ServicioCatalogo.Domain.Categories.Entities;
using ServicioCatalogo.Domain.Shared.Validation;

namespace ServicioCatalogo.Domain.Categories.Factories
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

