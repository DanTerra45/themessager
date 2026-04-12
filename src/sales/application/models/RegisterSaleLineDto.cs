using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.sales.application.models
{
    public sealed class RegisterSaleLineDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "El producto es obligatorio.")]
        public long ProductId { get; set; }

        [Range(1, 9999, ErrorMessage = "La cantidad debe estar entre 1 y 9999.")]
        public int Quantity { get; set; }
    }
}
