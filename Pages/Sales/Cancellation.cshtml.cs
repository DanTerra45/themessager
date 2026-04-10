using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Sales
{
    public class CancellationModel : PageModel
    {
        public int RequestCountToday { get; private set; }
        public int PendingCount { get; private set; }
        public decimal AmountUnderReview { get; private set; }

        public IReadOnlyList<CancellationRequestRow> CancellationRequests { get; private set; } = [];

        public void OnGet()
        {
            CancellationRequests =
            [
                new CancellationRequestRow("V-2026-00184", "Error de cobro en caja", "Mariana Choque", "2026-03-19 10:20", 245.50m, "Pendiente"),
                new CancellationRequestRow("V-2026-00179", "Cliente desistió de la compra", "Carlos Mamani", "2026-03-19 09:05", 67.00m, "Pendiente"),
                new CancellationRequestRow("V-2026-00172", "Producto duplicado en ticket", "Ana Perez", "2026-03-18 18:42", 112.30m, "Pendiente")
            ];

            RequestCountToday = 2;
            PendingCount = CancellationRequests.Count;
            AmountUnderReview = CancellationRequests.Sum(request => request.Amount);
        }
    }

    public sealed record CancellationRequestRow(
        string SaleCode,
        string Reason,
        string RequestedBy,
        string RequestedAt,
        decimal Amount,
        string Status);
}
