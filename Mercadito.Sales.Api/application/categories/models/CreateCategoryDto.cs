using Mercadito.src.domain.shared.validation;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.application.categories.models
{
    public class CreateCategoryDto : IValidatableObject
    {
        private const string CategoryCodePattern = "^C[0-9]{5}$";

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 caracteres")]
        [RegularExpression(CategoryCodePattern, ErrorMessage = "El código debe tener formato C00001")]
        public string Code { get; set; } = string.Empty;

        public CreateCategoryDto()
        {
        }

        public CreateCategoryDto(string name, string description, string code)
        {
            Name = name;
            Description = description;
            Code = code;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var normalizedName = ValidationText.NormalizeCollapsed(Name);
            var normalizedDescription = ValidationText.NormalizeTrimmed(Description);

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                yield return new ValidationResult("El nombre es obligatorio", [nameof(Name)]);
            }

            if (string.IsNullOrWhiteSpace(normalizedDescription))
            {
                yield return new ValidationResult("La descripción es obligatoria", [nameof(Description)]);
            }

            if (ContainsControlCharacters(normalizedName))
            {
                yield return new ValidationResult("El nombre contiene caracteres no permitidos", [nameof(Name)]);
            }

            if (ContainsControlCharacters(normalizedDescription))
            {
                yield return new ValidationResult("La descripción contiene caracteres no permitidos", [nameof(Description)]);
            }
        }

        private static bool ContainsControlCharacters(string value)
        {
            foreach (var character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
