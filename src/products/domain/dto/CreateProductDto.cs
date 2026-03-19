using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Mercadito.src.products.domain.dto
{
    public class CreateProductDto : IValidatableObject
    {
        private const string BatchPattern = "^[0-9]{1,40}$";

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Producto")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Display(Name = "Descripción del Producto")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Display(Name = "Stock Disponible")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número positivo")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El stock debe ser un número entero")]
        public int? Stock { get; set; }

        [Required(ErrorMessage = "Lote es obligatorio")]
        [Display(Name = "Lote")]
        [StringLength(40, ErrorMessage = "Lote no puede exceder 40 caracteres")]
        [RegularExpression(BatchPattern, ErrorMessage = "El lote solo permite números")]
        public string Batch { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de caducidad es obligatoria")]
        [Display(Name = "Fecha de Caducidad")]
        public DateOnly ExpirationDate { get; set; }

        [Display(Name = "Precio")]
        [Positive(FieldName = "Precio")]
        public decimal? Price { get; set; }

        [Display(Name = "Categorías")]
        public List<long> CategoryIds { get; set; } = [];

        public CreateProductDto()
        {
        }

        public CreateProductDto(
            string name,
            string description,
            int stock,
            string batch,
            DateOnly expirationDate,
            decimal price,
            IReadOnlyCollection<long> categoryIds)
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
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult("El nombre es obligatorio", [nameof(Name)]);
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                yield return new ValidationResult("La descripción es obligatoria", [nameof(Description)]);
            }

            if (ContainsControlCharacters(Name))
            {
                yield return new ValidationResult("El nombre contiene caracteres no permitidos", [nameof(Name)]);
            }

            if (ContainsControlCharacters(Description))
            {
                yield return new ValidationResult("La descripción contiene caracteres no permitidos", [nameof(Description)]);
            }

            if (string.IsNullOrWhiteSpace(Batch))
            {
                yield return new ValidationResult("Lote es obligatorio", [nameof(Batch)]);
            }

            if (ExpirationDate == default)
            {
                yield return new ValidationResult("La fecha de caducidad es obligatoria", [nameof(ExpirationDate)]);
            }
            else
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (ExpirationDate < today)
                {
                    yield return new ValidationResult("La fecha de caducidad no puede ser menor a hoy", [nameof(ExpirationDate)]);
                }
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
