using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.dto;
using Shared.Domain;
using System;
using System.Text.RegularExpressions;

namespace Mercadito.src.categories.domain.factory
{
    public class CategoryFactory : ICategoryFactory
    {
        private const string CategoryCodePattern = "^C[0-9]{5}$";
        private static readonly Regex CodeRegex = new Regex(CategoryCodePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public Category CreateForInsert(CreateCategoryDto dto)
        {
            return new Category
            {
                Code = string.Empty,
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

        public Result<Category> TryCreateForInsert(CreateCategoryDto dto)
        {
            if (dto == null) return Result<Category>.Failure("Category data is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result<Category>.Failure("El nombre es obligatorio.");
            if (dto.Name.Length > 150) return Result<Category>.Failure("El nombre no puede exceder 150 caracteres.");
            if (string.IsNullOrWhiteSpace(dto.Description)) return Result<Category>.Failure("La descripción es obligatoria.");
            if (dto.Description.Length > 150) return Result<Category>.Failure("La descripción no puede exceder 150 caracteres.");
            if (ContainsControlCharacters(dto.Name)) return Result<Category>.Failure("El nombre contiene caracteres no permitidos.");
            if (ContainsControlCharacters(dto.Description)) return Result<Category>.Failure("La descripción contiene caracteres no permitidos.");
            if (string.IsNullOrWhiteSpace(dto.Code)) return Result<Category>.Failure("El código es obligatorio.");
            if (!CodeRegex.IsMatch(dto.Code)) return Result<Category>.Failure("El código debe tener formato C00001.");

            var entity = CreateForInsert(dto);
            entity.Code = NormalizeCode(dto.Code);
            return Result<Category>.Success(entity);
        }

        public Result<Category> TryCreateForUpdate(UpdateCategoryDto dto)
        {
            if (dto == null) return Result<Category>.Failure("Category data is required.");
            if (dto.Id <= 0) return Result<Category>.Failure("Id de categoría inválido.");
            // reuse same validations as insert for fields
            return TryCreateForInsert(new CreateCategoryDto { Name = dto.Name, Description = dto.Description, Code = dto.Code });
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

        private static bool ContainsControlCharacters(string value)
        {
            foreach (var character in value)
            {
                if (char.IsControl(character)) return true;
            }

            return false;
        }
    }
}
