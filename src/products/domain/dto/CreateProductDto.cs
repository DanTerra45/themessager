using System.ComponentModel.DataAnnotations;

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
        public DateOnly Batch { get; set; }
        [Required(ErrorMessage = "La fecha de caducidad es obligatoria")]
        [Display(Name = "Fecha de Caducidad")]
        public DateOnly ExpirationDate { get; set; }
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(typeof(decimal), "0.01", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El precio debe estar entre 0.01 y 99999999.99")]
        public decimal Price { get; set; }

        [Display(Name = "Categorías")]
        public List<long> CategoryIds { get; set; } = [];
        
        public CreateProductDto() { }
        
        public CreateProductDto(string name, string description, int stock, DateOnly batch, DateOnly expirationDate, decimal price, IReadOnlyCollection<long> categoryIds)
        {
            Name = name;
            Description = description;
            Stock = stock;
            Batch = batch;
            ExpirationDate = expirationDate;
            Price = price;

            foreach (var categoryId in categoryIds)
            {
                CategoryIds.Add(categoryId);
            }
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

            if (Batch != default && ExpirationDate != default && ExpirationDate <= Batch)
            {
                yield return new ValidationResult("La fecha de caducidad debe ser posterior a la fecha del lote", [nameof(ExpirationDate)]);
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
