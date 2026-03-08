using System.ComponentModel.DataAnnotations;
using Mercadito.src.products.domain.validation;

namespace Mercadito.src.products.domain.dto
{
    public class CreateProductDto : IValidatableObject
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Producto")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Display(Name = "Descripción del Producto")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public required string Description { get; set; }
        [Required(ErrorMessage = "El stock es obligatorio")]
        [Display(Name = "Stock Disponible")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número positivo")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El stock debe ser un número entero")]
        public int Stock { get; set; }
        [Required(ErrorMessage = "La fecha de lote es obligatoria")]
        [Display(Name = "Fecha del Lote")]
        [DataType(DataType.Date)]
        public DateTime Batch { get; set; }
        [Required(ErrorMessage = "La fecha de caducidad es obligatoria")]
        [Display(Name = "Fecha de Caducidad")]
        [DateGreaterThan("Batch", ErrorMessage = "La fecha de caducidad debe ser posterior a la fecha del lote")]
        public DateTime ExpirationDate { get; set; }
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El precio debe estar entre 0.01 y 99999999.99")]
        public decimal Price { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "La categora seleccionada es invlida")]
        public long CategoryId { get; set; } = 0;
        
        public CreateProductDto() { }
        
        public CreateProductDto(string name, string description, int stock, DateTime batch, DateTime expirationDate, decimal price, long categoryId)
        {
            Name = name;
            Description = description;
            Stock = stock;
            Batch = batch;
            ExpirationDate = expirationDate;
            Price = price;
            CategoryId = categoryId;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Batch == default)
            {
                yield return new ValidationResult("La fecha de lote es obligatoria", [nameof(Batch)]);
            }

            if (ExpirationDate == default)
            {
                yield return new ValidationResult("La fecha de caducidad es obligatoria", [nameof(ExpirationDate)]);
            }
        }
    }

}
