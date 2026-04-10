using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Sales
{
    public class SalesModel : PageModel
    {
        public decimal VentasHoy { get; private set; }
        public decimal TicketPromedio { get; private set; }
        public int VentasRegistradasHoy { get; private set; }

        public IReadOnlyList<VentaRow> VentasRecientes { get; private set; } = [];

        public void OnGet()
        {
            VentasHoy = 4380.75m;
            TicketPromedio = 73.01m;
            VentasRegistradasHoy = 60;

            VentasRecientes =
            [
                new VentaRow("V-2026-00184", "2026-03-19 10:14", "Mostrador 1", "Efectivo", 245.50m),
                new VentaRow("V-2026-00183", "2026-03-19 10:02", "Mostrador 2", "QR", 98.00m),
                new VentaRow("V-2026-00182", "2026-03-19 09:48", "Mostrador 1", "Tarjeta", 156.25m),
                new VentaRow("V-2026-00181", "2026-03-19 09:30", "Mostrador 3", "Efectivo", 72.00m),
                new VentaRow("V-2026-00180", "2026-03-19 09:10", "Mostrador 2", "QR", 321.40m)
            ];
        }
    }

    public sealed record VentaRow(
        string Codigo,
        string FechaHora,
        string Canal,
        string MetodoPago,
        decimal Total);
}
