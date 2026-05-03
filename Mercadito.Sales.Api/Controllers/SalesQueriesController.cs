using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Contracts.Sales;
using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Mercadito.src.domain.shared;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Sales.Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesQueriesController(ISalesQueryFacade salesQueryFacade) : ControllerBase
{
    [HttpGet("context")]
    public async Task<ActionResult<ApiResponse<SalesRegistrationContextResponse>>> GetRegistrationContextAsync(
        [FromQuery] string customerSearchTerm = "",
        [FromQuery] string productSearchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.LoadRegistrationContextAsync(
            customerSearchTerm,
            productSearchTerm,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<SalesRegistrationContextResponse>(result));
        }

        return Ok(ApiResponse<SalesRegistrationContextResponse>.Ok(MapRegistrationContext(result.Value)));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerOptionResponse>>>> SearchCustomersAsync(
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.SearchCustomersAsync(searchTerm, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<IReadOnlyList<CustomerOptionResponse>>(result));
        }

        return Ok(ApiResponse<IReadOnlyList<CustomerOptionResponse>>.Ok(MapCustomers(result.Value)));
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SaleProductOptionResponse>>>> SearchProductsAsync(
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.SearchProductsAsync(searchTerm, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<IReadOnlyList<SaleProductOptionResponse>>(result));
        }

        return Ok(ApiResponse<IReadOnlyList<SaleProductOptionResponse>>.Ok(MapProducts(result.Value)));
    }

    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SaleSummaryResponse>>>> GetRecentSalesAsync(
        [FromQuery] int take = 20,
        [FromQuery] string sortBy = "createdat",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.GetRecentSalesAsync(take, sortBy, sortDirection, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<IReadOnlyList<SaleSummaryResponse>>(result));
        }

        return Ok(ApiResponse<IReadOnlyList<SaleSummaryResponse>>.Ok(MapSales(result.Value)));
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<SalesMetricsResponse>>> GetMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.GetOverviewMetricsAsync(cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<SalesMetricsResponse>(result));
        }

        return Ok(ApiResponse<SalesMetricsResponse>.Ok(MapMetrics(result.Value)));
    }

    [HttpGet("{saleId:long}")]
    public async Task<ActionResult<ApiResponse<SaleDetailResponse>>> GetSaleDetailAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.GetSaleDetailAsync(saleId, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ToFailure<SaleDetailResponse>(result));
        }

        return Ok(ApiResponse<SaleDetailResponse>.Ok(MapSaleDetail(result.Value)));
    }

    [HttpGet("{saleId:long}/receipt")]
    public async Task<ActionResult<ApiResponse<SaleReceiptResponse>>> GetSaleReceiptAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        var result = await salesQueryFacade.GetSaleReceiptAsync(saleId, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ToFailure<SaleReceiptResponse>(result));
        }

        return Ok(ApiResponse<SaleReceiptResponse>.Ok(MapSaleReceipt(result.Value)));
    }

    private static SalesRegistrationContextResponse MapRegistrationContext(SalesRegistrationContext context)
    {
        return new SalesRegistrationContextResponse(
            context.NextSaleCode,
            MapCustomers(context.Customers),
            MapProducts(context.Products));
    }

    private static IReadOnlyList<CustomerOptionResponse> MapCustomers(IReadOnlyList<CustomerLookupItem> customers)
    {
        return customers
            .Select(customer => new CustomerOptionResponse(
                customer.Id,
                customer.DocumentNumber,
                customer.BusinessName))
            .ToList();
    }

    private static IReadOnlyList<SaleProductOptionResponse> MapProducts(IReadOnlyList<SaleProductOption> products)
    {
        return products
            .Select(product => new SaleProductOptionResponse(
                product.Id,
                product.Name,
                product.Batch,
                product.Stock,
                product.Price))
            .ToList();
    }

    private static IReadOnlyList<SaleSummaryResponse> MapSales(IReadOnlyList<SaleSummaryItem> sales)
    {
        return sales
            .Select(sale => new SaleSummaryResponse(
                sale.Id,
                sale.Code,
                sale.CreatedAt,
                sale.CustomerName,
                sale.Channel,
                sale.PaymentMethod,
                sale.Total,
                sale.Status))
            .ToList();
    }

    private static SalesMetricsResponse MapMetrics(SalesOverviewMetrics metrics)
    {
        return new SalesMetricsResponse(
            metrics.RegisteredSalesCount,
            metrics.CancelledSalesCount,
            metrics.RegisteredAmountTotal,
            metrics.CancelledAmountTotal,
            metrics.SalesTodayCount,
            metrics.SalesTodayTotal,
            metrics.AverageTicketToday);
    }

    private static SaleDetailResponse MapSaleDetail(SaleDetailDto sale)
    {
        return new SaleDetailResponse(
            sale.Id,
            sale.Code,
            sale.CreatedAt,
            sale.CustomerDocumentNumber,
            sale.CustomerName,
            sale.Channel,
            sale.PaymentMethod,
            sale.Status,
            sale.Total,
            sale.Lines
                .Select(line => new SaleDetailLineResponse(
                    line.ProductId,
                    line.ProductName,
                    line.Batch,
                    line.Quantity,
                    line.UnitPrice,
                    line.Amount))
                .ToList());
    }

    private static SaleReceiptResponse MapSaleReceipt(SaleReceiptDto receipt)
    {
        return new SaleReceiptResponse(
            receipt.Id,
            receipt.Code,
            receipt.CreatedAt,
            receipt.GeneratedAt,
            receipt.CustomerDocumentNumber,
            receipt.CustomerName,
            receipt.CreatedByUsername,
            receipt.Total,
            receipt.AmountInWords,
            receipt.Lines
                .Select(line => new SaleReceiptLineResponse(
                    line.Description,
                    line.Quantity,
                    line.UnitPrice,
                    line.Amount))
                .ToList());
    }

    private static ApiResponse<T> ToFailure<T>(Result result)
    {
        return ApiResponse<T>.Fail(GetErrors(result).ToArray());
    }

    private static IReadOnlyList<string> GetErrors(Result result)
    {
        if (result.Errors.Count == 0)
        {
            return [result.ErrorMessage];
        }

        return result.Errors
            .SelectMany(error => error.Value)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .DefaultIfEmpty(result.ErrorMessage)
            .ToList();
    }
}
