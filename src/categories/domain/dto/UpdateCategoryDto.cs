using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.categories.domain.dto
{
    
    public class UpdateCategoryDto
    {
        [Required]
        public long Id { get; set; }
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(6, ErrorMessage = "El código no puede exceder 6 caracteres")]
        public required string Code { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public required string Name { get; set; }
        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public required string Description { get; set; }
        public UpdateCategoryDto() { }
        public UpdateCategoryDto(long id, string code, string name, string description)
        {
            Id = id;
            Code = code;
            Name = name;
            Description = description;
        }
    }
}