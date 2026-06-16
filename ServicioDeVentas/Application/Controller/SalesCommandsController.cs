using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Service;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicioVentas.Contracts.Common;
using ServicioVentas.Contracts.Sales;

namespace Application.Controller;

[ApiController]
[Route("api/sales")]
[Authorize(Policy = "SalesOperator")]
public sealed class SalesCommandsController(SalesContractService salesContractService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SaleReceiptResponse>>> RegisterAsync(
        [FromBody] RegisterSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.RegisterSaleAsync(request, BuildActor(), cancellationToken);
        if (result.IsSuccess)
        {
            return Created(
                $"/api/sales/{result.Value.Id.ToString(CultureInfo.InvariantCulture)}/receipt",
                ApiResponse<SaleReceiptResponse>.Ok(result.Value));
        }

        var statusCode = ResolveStatusCode(result.Errors);
        return StatusCode(statusCode, ToFailure(result));
    }

    [HttpPost("{saleId:long}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelAsync(
        long saleId,
        [FromBody] CancelSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await salesContractService.CancelSaleAsync(saleId, request.Reason, BuildActor(), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<bool>.Ok(true));
        }

        var statusCode = ResolveStatusCode(result.Errors);
        return StatusCode(statusCode, ToFailure(result));
    }

    private SalesContractService.SalesActor BuildActor()
    {
        return new SalesContractService.SalesActor(ResolveUserId(), ResolveUsername());
    }

    private long ResolveUserId()
    {
        var candidates = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            Request.Headers["X-User-Id"].FirstOrDefault()
        };

        foreach (var candidate in candidates)
        {
            if (long.TryParse(candidate, out var userId) && userId > 0)
            {
                return userId;
            }
        }

        return 1;
    }

    private string ResolveUsername()
    {
        if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
        {
            return User.Identity.Name!;
        }

        var preferredUsername = User.FindFirstValue("preferred_username");
        if (!string.IsNullOrWhiteSpace(preferredUsername))
        {
            return preferredUsername;
        }

        var username = Request.Headers["X-Username"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(username) ? "frontend" : username;
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
