using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.application.sales.models
{
    public sealed class UpdateSaleDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "La venta es obligatoria.")]
        public long SaleId { get; set; }

        [Range(0, long.MaxValue)]
        public long CustomerId { get; set; }

        public CreateCustomerDto NewCustomer { get; set; } = new();

        [Required(ErrorMessage = "El canal es obligatorio.")]
        [StringLength(30, ErrorMessage = "El canal no puede exceder 30 caracteres.")]
        public string Channel { get; set; } = "Mostrador";

        [Required(ErrorMessage = "El método de pago es obligatorio.")]
        [StringLength(30, ErrorMessage = "El método de pago no puede exceder 30 caracteres.")]
        public string PaymentMethod { get; set; } = string.Empty;

        public List<RegisterSaleLineDto> Lines { get; set; } = [];
    }
}
