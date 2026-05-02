using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Sales;

namespace Mercadito.Frontend.Adapters.Sales;

public interface ISalesApiAdapter
{
    Task<ApiResponseDto<SalesRegistrationContextDto>> GetRegistrationContextAsync(
        string customerSearchTerm = "",
        string productSearchTerm = "",
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<IReadOnlyList<CustomerOptionDto>>> SearchCustomersAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<IReadOnlyList<SaleProductOptionDto>>> SearchProductsAsync(
        string searchTerm = "",
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<IReadOnlyList<SaleSummaryDto>>> GetRecentSalesAsync(
        int take = 20,
        string sortBy = "createdat",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<SalesMetricsDto>> GetMetricsAsync(CancellationToken cancellationToken = default);

    Task<ApiResponseDto<SaleDetailDto>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default);

    Task<ApiResponseDto<SaleReceiptDto>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default);

    Task<ApiResponseDto<SaleReceiptDto>> RegisterSaleAsync(
        RegisterSaleRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CancelSaleAsync(
        long saleId,
        string reason,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);
}
