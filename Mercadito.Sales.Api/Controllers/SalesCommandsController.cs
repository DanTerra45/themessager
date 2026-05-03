using System.Security.Claims;
using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Contracts.Sales;
using Mercadito.src.application.sales.models;
using Mercadito.src.application.sales.ports.input;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Sales.Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesCommandsController(
    IRegisterSaleFacade registerSaleFacade,
    ICancelSaleFacade cancelSaleFacade) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SaleReceiptResponse>>> RegisterAsync(
        RegisterSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await registerSaleFacade.RegisterAsync(
            MapRegisterRequest(request),
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<SaleReceiptResponse>(result));
        }

        return Created(
            $"/api/sales/{result.Value.Id}/receipt",
            ApiResponse<SaleReceiptResponse>.Ok(MapReceipt(result.Value)));
    }

    [HttpPost("{saleId:long}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelAsync(
        long saleId,
        CancelSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await cancelSaleFacade.CancelAsync(
            new CancelSaleDto
            {
                SaleId = saleId,
                Reason = request.Reason
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    private static RegisterSaleDto MapRegisterRequest(RegisterSaleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RegisterSaleDto
        {
            CustomerId = request.CustomerId ?? 0,
            NewCustomer = request.NewCustomer == null
                ? new CreateCustomerDto()
                : new CreateCustomerDto
                {
                    DocumentNumber = request.NewCustomer.CiNit,
                    BusinessName = request.NewCustomer.BusinessName,
                    Phone = request.NewCustomer.Phone,
                    Email = request.NewCustomer.Email,
                    Address = request.NewCustomer.Address
                },
            Channel = request.Channel,
            PaymentMethod = request.PaymentMethod,
            Lines = request.Lines
                .Select(line => new RegisterSaleLineDto
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity
                })
                .ToList()
        };
    }

    private static SaleReceiptResponse MapReceipt(SaleReceiptDto receipt)
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

    private AuditActor BuildActor()
    {
        var userId = ResolveUserId();
        var username = ResolveUsername();

        return new AuditActor
        {
            UserId = userId,
            Username = username,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
    }

    private long ResolveUserId()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdText, out var userId) && userId > 0)
        {
            return userId;
        }

        userIdText = Request.Headers["X-User-Id"].FirstOrDefault();
        if (long.TryParse(userIdText, out userId) && userId > 0)
        {
            return userId;
        }

        return 1;
    }

    private string ResolveUsername()
    {
        if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
        {
            return User.Identity.Name;
        }

        var username = Request.Headers["X-Username"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        return "frontend";
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
