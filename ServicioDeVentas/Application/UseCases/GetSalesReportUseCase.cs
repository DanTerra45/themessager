using Domain.Common;
using Domain.Dto.Response;
using Domain.Entities.Enums;

namespace Application.UseCases;

public sealed class GetSalesReportUseCase
{
    private readonly GetAllSalesUseCase _getAllSalesUseCase;

    public GetSalesReportUseCase(GetAllSalesUseCase getAllSalesUseCase)
    {
        _getAllSalesUseCase = getAllSalesUseCase;
    }

    public async Task<Result<SalesReportResponseDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var salesResult = await _getAllSalesUseCase.ExecuteAsync(null);
        if (!salesResult.IsSuccess || salesResult.Value is null)
        {
            return Result<SalesReportResponseDto>.Failure(salesResult.Errors);
        }

        var sales = salesResult.Value;
        var totalAmount = sales.Sum(sale => sale.TotalPrice);
        var cancelledAmount = sales.Where(sale => sale.State == SaleState.Cancelled).Sum(sale => sale.TotalPrice);
        var products = sales
            .SelectMany(sale => sale.Details)
            .GroupBy(detail => detail.ProductId)
            .Select(group => new SalesReportProductDto(
                ProductId: group.Key,
                Quantity: group.Sum(detail => detail.Quantity),
                Amount: group.Sum(detail => detail.SubTotal)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.ProductId)
            .ToList();

        return Result<SalesReportResponseDto>.Success(new SalesReportResponseDto(
            GeneratedAt: DateTime.UtcNow,
            TotalSalesCount: sales.Count,
            PendingSalesCount: sales.Count(sale => sale.State == SaleState.Pending),
            ConfirmedSalesCount: sales.Count(sale => sale.State == SaleState.Confirmed),
            CancelledSalesCount: sales.Count(sale => sale.State == SaleState.Cancelled),
            TotalAmount: totalAmount,
            CancelledAmount: cancelledAmount,
            AverageTicket: sales.Count == 0 ? 0 : totalAmount / sales.Count,
            Products: products));
    }
}