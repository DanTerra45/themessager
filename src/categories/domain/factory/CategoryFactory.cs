using Mercadito.src.categories.data.entity;
using Mercadito.src.categories.domain.dto;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mercadito.src.categories.domain.factory
{
    public class CategoryFactory : ICategoryFactory
    {
        public Category CreateForInsert(CreateCategoryDto dto)
        {
            return new Category
            {
                Code = GenerateOrNormalizeCode(dto.Code, dto.Name),
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

        private static string GenerateOrNormalizeCode(string providedCode, string name)
        {
            if (!string.IsNullOrWhiteSpace(providedCode))
            {
                return NormalizeCode(providedCode);
            }

            // Generar: 3 letras de nombre (sin acentos, no letras extra) + 3 dígitos aleatorios
            var firstThree = ExtractLetters(name).ToUpperInvariant();
            if (firstThree.Length < 3)
            {
                firstThree = firstThree.PadRight(3, 'X');
            }
            else
            {
                firstThree = firstThree.Substring(0, 3);
            }

            var rand = new Random();
            var number = rand.Next(0, 1000);
            return $"{firstThree}{number:000}";
        }

        private static string ExtractLetters(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "XXX";
            }

            // Remover diacríticos
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark && char.IsLetter(ch))
                {
                    sb.Append(ch);
                }
            }

            var result = sb.ToString().Normalize(NormalizationForm.FormC);
            // Quedarse solo con letras (a–z, A–Z)
            result = Regex.Replace(result, "[^A-Za-z]", string.Empty);
            return string.IsNullOrEmpty(result) ? "XXX" : result;
        }

        private static string NormalizeName(string value)
        {
            var normalizedValue = NormalizeRequired(value);
            var segments = normalizedValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', segments);
        }
    }
}
