using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.categories.domain.dto
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public required string Description { get; set; }
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(6, ErrorMessage = "El código no puede exceder 6 caracteres")]
        public required string Code { get; set; }
        public CreateCategoryDto() { }
        public CreateCategoryDto(string name, string description, string code)
        {
            Name = name;
            Description = description;
            Code = code;
        }
    }
}