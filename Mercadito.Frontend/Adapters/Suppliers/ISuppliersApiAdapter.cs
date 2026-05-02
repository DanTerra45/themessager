using Mercadito.Frontend.Dtos.Common;
using Mercadito.Frontend.Dtos.Suppliers;

namespace Mercadito.Frontend.Adapters.Suppliers;

public interface ISuppliersApiAdapter
{
    Task<ApiResponseDto<SupplierPageDto>> GetSuppliersAsync(
        string searchTerm,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<SupplierDto>> GetSupplierAsync(
        long supplierId,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> CreateSupplierAsync(
        SaveSupplierRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> UpdateSupplierAsync(
        long supplierId,
        SaveSupplierRequestDto request,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);

    Task<ApiResponseDto<bool>> DeleteSupplierAsync(
        long supplierId,
        ApiActorContextDto actor,
        CancellationToken cancellationToken = default);
}
