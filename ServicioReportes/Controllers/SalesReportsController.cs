using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioReportes.Application.Reports.Ports.Input;
using ServicioReportes.Contracts.Common;
using ServicioReportes.Contracts.Sales;
using ServicioReportes.Domain.Shared;

namespace ServicioReportes.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Policy = "ReportsViewer")]
public sealed class SalesReportsController(IReportsReadUseCase reportsReadUseCase) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<SalesMetricsResponse>>> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var result = await reportsReadUseCase.GetMetricsAsync(cancellationToken);
        if (result.IsFailure)
        {
            return result.Errors.Count > 0
                ? BadRequest(ApiResponse<SalesMetricsResponse>.Fail(result.Errors))
                : BadRequest(ApiResponse<SalesMetricsResponse>.Fail(result.ErrorMessage));
        }

        return Ok(ApiResponse<SalesMetricsResponse>.Ok(new SalesMetricsResponse(
            result.Value.RegisteredSales,
            result.Value.CancelledSales,
            result.Value.RegisteredAmount,
            result.Value.CancelledAmount,
            result.Value.SalesToday,
            result.Value.SalesTodayAmount,
            result.Value.AverageTicket)));
    }

    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SaleSummaryResponse>>>> GetRecentSalesAsync([FromQuery] int take = 20, [FromQuery] string sortBy = "createdat", [FromQuery] string sortDirection = "desc", [FromQuery] DateOnly? fromDate = null, [FromQuery] DateOnly? toDate = null, [FromQuery] string status = "", [FromQuery] string paymentMethod = "", CancellationToken cancellationToken = default)
    {
        var result = await reportsReadUseCase.GetRecentSalesAsync(take, sortBy, sortDirection, fromDate, toDate, status, paymentMethod, cancellationToken);
        if (result.IsFailure)
        {
            return result.Errors.Count > 0
                ? BadRequest(ApiResponse<IReadOnlyList<SaleSummaryResponse>>.Fail(result.Errors))
                : BadRequest(ApiResponse<IReadOnlyList<SaleSummaryResponse>>.Fail(result.ErrorMessage));
        }

        var payload = result.Value.Select(sale => new SaleSummaryResponse(sale.Id, sale.Code, sale.CreatedAt, sale.CustomerName, sale.Channel, sale.PaymentMethod, sale.Total, sale.Status)).ToList();
        return Ok(ApiResponse<IReadOnlyList<SaleSummaryResponse>>.Ok(payload));
    }

    [HttpGet("reports/daily-cash-closing")]
    public async Task<ActionResult<ApiResponse<DailyCashClosingReportResponse>>> GetDailyCashClosingAsync([FromQuery] DateOnly businessDate, CancellationToken cancellationToken)
    {
        var result = await reportsReadUseCase.GetDailyCashClosingReportAsync(businessDate, cancellationToken);
        if (result.IsFailure)
        {
            return result.Errors.Count > 0
                ? BadRequest(ApiResponse<DailyCashClosingReportResponse>.Fail(result.Errors))
                : BadRequest(ApiResponse<DailyCashClosingReportResponse>.Fail(result.ErrorMessage));
        }

        var payload = new DailyCashClosingReportResponse(
            result.Value.BusinessDate,
            result.Value.GeneratedAt,
            result.Value.RegisteredSalesCount,
            result.Value.CancelledSalesCount,
            result.Value.RegisteredAmountTotal,
            result.Value.CancelledAmountTotal,
            result.Value.AverageTicket,
            result.Value.PaymentMethods.Select(method => new DailyPaymentMethodSummaryResponse(method.PaymentMethod, method.SalesCount, method.TotalAmount)).ToList(),
            result.Value.Products.Select(product => new DailyProductSalesSummaryResponse(product.ProductId, product.ProductName, product.QuantitySold, product.AverageUnitPrice, product.TotalAmount)).ToList());

        return Ok(ApiResponse<DailyCashClosingReportResponse>.Ok(payload));
    }

    [HttpGet("{saleId:long}")]
    public async Task<ActionResult<ApiResponse<SaleDetailResponse>>> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken)
    {
        var result = await reportsReadUseCase.GetSaleDetailAsync(saleId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Errors.Count > 0)
            {
                return BadRequest(ApiResponse<SaleDetailResponse>.Fail(result.Errors));
            }

            return NotFound(ApiResponse<SaleDetailResponse>.Fail(result.ErrorMessage));
        }

        var payload = new SaleDetailResponse(
            result.Value.Id,
            result.Value.Code,
            result.Value.CreatedAt,
            result.Value.CustomerCiNit,
            result.Value.CustomerBusinessName,
            result.Value.Channel,
            result.Value.PaymentMethod,
            result.Value.Status,
            result.Value.Total,
            result.Value.Lines.Select(line => new SaleDetailLineResponse(line.ProductId, line.ProductName, line.LotCode, line.Quantity, line.UnitPrice, line.Subtotal)).ToList());

        return Ok(ApiResponse<SaleDetailResponse>.Ok(payload));
    }

    [HttpGet("{saleId:long}/receipt")]
    public async Task<ActionResult<ApiResponse<SaleReceiptResponse>>> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken)
    {
        var result = await reportsReadUseCase.GetSaleReceiptAsync(saleId, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Errors.Count > 0)
            {
                return BadRequest(ApiResponse<SaleReceiptResponse>.Fail(result.Errors));
            }

            return NotFound(ApiResponse<SaleReceiptResponse>.Fail(result.ErrorMessage));
        }

        var payload = new SaleReceiptResponse(
            result.Value.Id,
            result.Value.Code,
            result.Value.CreatedAt,
            result.Value.GeneratedAt,
            result.Value.CustomerCiNit,
            result.Value.CustomerBusinessName,
            result.Value.CreatedBy,
            result.Value.Total,
            result.Value.AmountInWords,
            result.Value.Lines.Select(line => new SaleReceiptLineResponse(line.ProductName, line.Quantity, line.UnitPrice, line.Subtotal)).ToList());

        return Ok(ApiResponse<SaleReceiptResponse>.Ok(payload));
    }
}
