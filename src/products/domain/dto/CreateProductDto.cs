using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Mercadito.src.products.domain.dto
{
    public class CreateProductDto : IValidatableObject
    {
        private const string BatchPattern = "^[A-Za-z0-9][A-Za-z0-9 ._/-]{0,39}$";

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Producto")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "La descripcion es obligatoria")]
        [Display(Name = "Descripcion del Producto")]
        [StringLength(150, ErrorMessage = "La descripcion no puede exceder 150 caracteres")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Display(Name = "Stock Disponible")]
<<<<<<< HEAD
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un numero positivo")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El stock debe ser un numero entero")]
        public int Stock { get; set; }

=======
        [Positive(ErrorMessage = "El stock debe ser un número positivo")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El stock debe ser un número entero")]
        public int? Stock { get; set; }
>>>>>>> 374af05 (refactor: Validaciones mas descriptivas usando data anotation y tomando en cuenta casos especiales)
        [Required(ErrorMessage = "Lote es obligatorio")]
        [Display(Name = "Lote")]
        [StringLength(40, ErrorMessage = "Lote no puede exceder 40 caracteres")]
        [RegularExpression(BatchPattern, ErrorMessage = "El lote solo permite letras, numeros, espacio, punto, guion, guion bajo y barra")]
        public string Batch { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de caducidad es obligatoria")]
        [Display(Name = "Fecha de Caducidad")]
        public DateOnly ExpirationDate { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Display(Name = "Precio")]
        [Positive(ErrorMessage = "El precio debe ser un número positivo")]
        public decimal? Price { get; set; }

        [Display(Name = "Categorias")]
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
                yield return new ValidationResult("La fecha de caducidad es obligatoria", [nameof(ExpirationDate)]);
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
