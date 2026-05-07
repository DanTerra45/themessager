using Mercadito.Frontend.Dtos.Sales;

namespace Mercadito.Frontend.Pages.Sales;

public interface IDailyCashClosingExcelExporter
{
    byte[] Export(DailyCashClosingReportDto report);
}
