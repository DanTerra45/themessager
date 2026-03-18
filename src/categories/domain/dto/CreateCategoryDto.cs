using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.categories.domain.dto
{
    public class CreateCategoryDto : IValidatableObject
    {
        private const string CategoryCodePattern = "^[A-Z]{3}\\d{3}$";

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(6, ErrorMessage = "El código no puede exceder 6 caracteres")]
        [RegularExpression(CategoryCodePattern, ErrorMessage = "El código solo permite letras mayúsculas y números, con el formato 3 letras + 3 números (ejemplo: ABC123)")]
        public required string Code { get; set; }

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
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult("El nombre es obligatorio", [nameof(Name)]);
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                yield return new ValidationResult("La descripción es obligatoria", [nameof(Description)]);
            }

            if (string.IsNullOrWhiteSpace(Code))
            {
                yield return new ValidationResult("El código es obligatorio", [nameof(Code)]);
            }

            if (ContainsControlCharacters(Name))
            {
                yield return new ValidationResult("El nombre contiene caracteres no permitidos", [nameof(Name)]);
            }

            if (ContainsControlCharacters(Description))
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
