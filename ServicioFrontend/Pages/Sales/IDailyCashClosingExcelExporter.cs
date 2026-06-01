using ServicioFrontend.Dtos.Sales;

namespace ServicioFrontend.Pages.Sales;

public interface IDailyCashClosingExcelExporter
{
    byte[] Export(DailyCashClosingReportDto report);
}
