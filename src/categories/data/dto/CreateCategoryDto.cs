using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(6, ErrorMessage = "El código no puede exceder 6 caracteres")]
        public string Code { get; set; } = string.Empty;
        public CreateCategoryDto() { }
        public CreateCategoryDto(string name, string description, string code)
        {
            Name = name;
            Description = description;
            Code = code;
        }
    }
}