using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.dto
{
    public class UpdateProductDto : IValidatableObject
    {
        private const string BatchPattern = "^[A-Za-z0-9][A-Za-z0-9 ._/-]{0,39}$";

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Id de producto invalido")]
        public long Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }

        [Display(Name = "Categorias")]
        public List<long> CategoryIds { get; set; } = [];

        [Required(ErrorMessage = "La descripcion es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripcion no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Positive(FieldName = "Stock")]
        public int? Stock { get; set; }

        [Required(ErrorMessage = "Lote es obligatorio")]
        [StringLength(40, ErrorMessage = "Lote no puede exceder 40 caracteres")]
        [RegularExpression(BatchPattern, ErrorMessage = "El lote solo permite letras, numeros, espacio, punto, guion, guion bajo y barra")]
        public string Batch { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fecha de vencimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateOnly ExpirationDate { get; set; }
        
        [Positive(FieldName = "Precio")]
        public decimal? Price { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult("El nombre es obligatorio", [nameof(Name)]);
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                yield return new ValidationResult("La descripcion es obligatoria", [nameof(Description)]);
            }

            if (ContainsControlCharacters(Name))
            {
                yield return new ValidationResult("El nombre contiene caracteres no permitidos", [nameof(Name)]);
            }

            if (ContainsControlCharacters(Description))
            {
                yield return new ValidationResult("La descripcion contiene caracteres no permitidos", [nameof(Description)]);
            }

            if (string.IsNullOrWhiteSpace(Batch))
            {
                yield return new ValidationResult("Lote es obligatorio", [nameof(Batch)]);
            }

            if (ExpirationDate == default)
            {
                yield return new ValidationResult("Fecha de vencimiento es obligatoria", [nameof(ExpirationDate)]);
            }

            var distinctCategoryIds = new HashSet<long>();
            foreach (var categoryId in CategoryIds)
            {
                if (categoryId <= 0)
                {
                    yield return new ValidationResult("Las categorias seleccionadas son invalidas", [nameof(CategoryIds)]);
                    yield break;
                }

                if (!distinctCategoryIds.Add(categoryId))
                {
                    yield return new ValidationResult("No puede repetir categorias para el mismo producto", [nameof(CategoryIds)]);
                    yield break;
                }
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
