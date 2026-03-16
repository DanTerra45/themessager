
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.products.domain.dto
{
    public class UpdateProductDto : IValidatableObject
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Id de producto invlido")]
        public long Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }

        [Display(Name = "Categorías")]
        public List<long> CategoryIds { get; set; } = [];

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripcion no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Stock es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock debe ser 0 o mayor")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Lote es obligatorio")]
        [StringLength(40, ErrorMessage = "Lote no puede exceder 40 caracteres")]
        public string Batch { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fecha de vencimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateOnly ExpirationDate { get; set; }

        [Required(ErrorMessage = "Precio es obligatorio")]
        [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Precio invalido")]
        public decimal Price { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
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
                    yield return new ValidationResult("Las categorías seleccionadas son inválidas", [nameof(CategoryIds)]);
                    yield break;
                }

                if (!distinctCategoryIds.Add(categoryId))
                {
                    yield return new ValidationResult("No puede repetir categorías para el mismo producto", [nameof(CategoryIds)]);
                    yield break;
                }
            }
        }
    }
}
