using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages.Sales
{
    public class ReportsModel : PageModel
    {
        public int DailyReports { get; private set; }
        public int WeeklyReports { get; private set; }
        public int MonthlyReports { get; private set; }
        public int ExportsToday { get; private set; }

        public IReadOnlyList<ReportRow> ReportRows { get; private set; } = [];

        public void OnGet()
        {
            DailyReports = 4;
            WeeklyReports = 2;
            MonthlyReports = 1;
            ExportsToday = 3;

            ReportRows =
            [
                new ReportRow("Ventas por producto", "Diario", "PDF", "2026-03-19 10:35"),
                new ReportRow("Resumen por categoría", "Semanal", "Excel", "2026-03-18 18:20"),
                new ReportRow("Rendimiento comercial", "Mensual", "PDF", "2026-03-01 09:10"),
                new ReportRow("Detalle por caja", "Diario", "Excel", "2026-03-19 09:45")
            ];
        }
    }

    public sealed record ReportRow(
        string ReportName,
        string Period,
        string Format,
        string LastGeneratedAt);
}
