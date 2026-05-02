using System.ComponentModel.DataAnnotations;

namespace Mercadito.src.application.sales.models
{
    public sealed class CancelSaleDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "La venta es obligatoria.")]
        public long SaleId { get; set; }

        [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
        [StringLength(255, ErrorMessage = "El motivo de anulación no puede exceder 255 caracteres.")]
        public string Reason { get; set; } = string.Empty;
    }
}
