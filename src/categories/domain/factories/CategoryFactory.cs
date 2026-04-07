using Mercadito.src.categories.domain.entities;
using Mercadito.src.categories.application.models;
using System;

namespace Mercadito.src.categories.domain.factories
{
    public class CategoryFactory : ICategoryFactory
    {
        public Category CreateForInsert(CreateCategoryDto dto)
        {
            return new Category
            {
                Code = NormalizeCode(dto.Code),
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description)
            };
        }

        public Category CreateForUpdate(UpdateCategoryDto dto)
        {
            return new Category
            {
                Id = dto.Id,
                Code = NormalizeCode(dto.Code),
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description)
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

        private static string NormalizeCode(string value)
        {
            var normalizedValue = NormalizeRequired(value);
            return normalizedValue.ToUpperInvariant();
        }

        private static string NormalizeName(string value)
        {
            var normalizedValue = NormalizeRequired(value);
            var segments = normalizedValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', segments);
        }
    }
}
