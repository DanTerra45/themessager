using Mercadito.Frontend.Dtos.Sales;

namespace Mercadito.Frontend.Pages.Sales;

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
