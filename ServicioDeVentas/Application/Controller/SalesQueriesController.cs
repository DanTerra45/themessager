using Application.Service;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioVentas.Contracts.Common;
using ServicioVentas.Contracts.Sales;

namespace Application.Controller;

[ApiController]
[Route("api/sales")]
[Authorize(Policy = "SalesViewer")]
public sealed class SalesQueriesController(SalesContractService salesContractService) : ControllerBase
{
    [HttpGet("context")]
    public async Task<ActionResult<ApiResponse<SalesRegistrationContextResponse>>> GetRegistrationContextAsync(
        [FromQuery] string customerSearchTerm = "",
        [FromQuery] string productSearchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.GetRegistrationContextAsync(customerSearchTerm, productSearchTerm, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("customers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerOptionResponse>>>> SearchCustomersAsync(
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.SearchCustomersAsync(searchTerm, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SaleProductOptionResponse>>>> SearchProductsAsync(
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.SearchProductsAsync(searchTerm, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SaleSummaryResponse>>>> GetRecentSalesAsync(
        [FromQuery] int take = 20,
        [FromQuery] string sortBy = "createdat",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] string status = "",
        [FromQuery] string paymentMethod = "",
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.GetRecentSalesAsync(
            take,
            sortBy,
            sortDirection,
            fromDate,
            toDate,
            status,
            paymentMethod,
            cancellationToken);

        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<SalesMetricsResponse>>> GetMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.GetMetricsAsync(cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("reports/daily-cash-closing")]
    public async Task<ActionResult<ApiResponse<DailyCashClosingReportResponse>>> GetDailyCashClosingReportAsync(
        [FromQuery] DateOnly? businessDate = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveBusinessDate = businessDate ?? DateOnly.FromDateTime(DateTime.Today);
        var result = await salesContractService.GetDailyCashClosingReportAsync(effectiveBusinessDate, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("{saleId:long}")]
    public async Task<ActionResult<ApiResponse<SaleDetailResponse>>> GetSaleDetailAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.GetSaleDetailAsync(saleId, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    [HttpGet("{saleId:long}/receipt")]
    public async Task<ActionResult<ApiResponse<SaleReceiptResponse>>> GetSaleReceiptAsync(
        long saleId,
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.GetSaleReceiptAsync(saleId, cancellationToken);
        return ToActionResult(result, StatusCodes.Status200OK);
    }

    private ActionResult<ApiResponse<T>> ToActionResult<T>(Result<T> result, int successStatusCode)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, ApiResponse<T>.Ok(result.Value));
        }

        var statusCode = ResolveStatusCode(result.Errors);
        return StatusCode(statusCode, ToFailure<T>(result));
    }

    private static ApiResponse<T> ToFailure<T>(Result<T> result)
    {
        var validationErrors = result.Errors
            .Where(error => error.Type == ErrorType.Validation && !string.IsNullOrWhiteSpace(error.Field))
            .GroupBy(error => error.Field!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(error => error.Message).Distinct(StringComparer.Ordinal).ToList(),
                StringComparer.OrdinalIgnoreCase);

        return validationErrors.Count > 0
            ? ApiResponse<T>.Fail(validationErrors)
            : ApiResponse<T>.Fail(result.Errors.Select(error => error.Message).ToArray());
    }

    private static int ResolveStatusCode(IReadOnlyCollection<AppError> errors)
    {
        if (errors.Any(error => error.Type == ErrorType.NotFound))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errors.Any(error => error.Type == ErrorType.Forbidden))
        {
            return StatusCodes.Status403Forbidden;
        }

        if (errors.Any(error => error.Type == ErrorType.Unauthorized))
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (errors.Any(error => error.Type == ErrorType.Validation || error.Type == ErrorType.Conflict))
        {
            return StatusCodes.Status400BadRequest;
        }

        return StatusCodes.Status500InternalServerError;
    }
}
