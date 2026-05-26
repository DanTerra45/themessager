using ServicioFrontend.Dtos.Sales;

namespace ServicioFrontend.Pages.Sales;

public interface ISalesListingExcelExporter
{
    byte[] Export(IReadOnlyList<SaleSummaryDto> sales, SalesListingExcelExportContext context);
}

public sealed record SalesListingExcelExportContext(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string Status,
    string PaymentMethod,
    DateTime GeneratedAt);
