using Mercadito.src.application.users.validation;
using Mercadito.src.domain.shared.validation;
using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.application.products.models
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
        public ICollection<long> CategoryIds { get; set; } = [];

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
            ArgumentNullException.ThrowIfNull(categoryIds);

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
            var normalizedName = ValidationText.NormalizeCollapsed(Name);
            var normalizedDescription = ValidationText.NormalizeTrimmed(Description);
            var normalizedBatch = ValidationText.NormalizeTrimmed(Batch);

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

            if (string.IsNullOrWhiteSpace(normalizedBatch))
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

            if (CategoryIds.Count == 0)
            {
                yield return new ValidationResult("Debe seleccionar al menos una categoría", [nameof(CategoryIds)]);
                yield break;
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
