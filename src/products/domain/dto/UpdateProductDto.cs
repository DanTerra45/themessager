
using System.ComponentModel.DataAnnotations;
using Mercadito.src.products.domain.validation;

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

        [Range(0, long.MaxValue, ErrorMessage = "La categora seleccionada es invlida")]
        public long CategoryId { get; set; } = 0;

        [StringLength(150, ErrorMessage = "La descripcion no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Stock es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock debe ser 0 o mayor")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Lote es obligatorio")]
        [DataType(DataType.Date)]
        public DateTime Batch { get; set; }

        [Required(ErrorMessage = "Fecha de vencimiento es obligatoria")]
        [DataType(DataType.Date)]
        [DateGreaterThan("Batch", ErrorMessage = "La fecha de caducidad debe ser posterior a la fecha del lote")]
        public DateTime ExpirationDate { get; set; }

        [Required(ErrorMessage = "Precio es obligatorio")]
        [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "Precio invalido")]
        public decimal Price { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Batch == default)
            {
                yield return new ValidationResult("Lote es obligatorio", [nameof(Batch)]);
            }

            if (ExpirationDate == default)
            {
                yield return new ValidationResult("Fecha de vencimiento es obligatoria", [nameof(ExpirationDate)]);
            }
        }
    }
}
