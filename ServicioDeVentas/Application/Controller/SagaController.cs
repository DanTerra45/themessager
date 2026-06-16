using System.Globalization;
using Domain.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controller;

[ApiController]
[Route("api/sagas")]
[Authorize(Policy = "SalesOperator")]
public sealed class SagaController(IEventPublisher eventPublisher) : ControllerBase
{
    [HttpPost("stock/reserve")]
    public async Task<IActionResult> ReserveStockAsync([FromBody] StockSagaRequest request, CancellationToken cancellationToken)
    {
        await PublishAsync(StockSagaRoutingKeys.ReserveRequested, request, cancellationToken);
        return Accepted();
    }

    [HttpPost("stock/recover")]
    public async Task<IActionResult> RecoverStockAsync([FromBody] StockSagaRequest request, CancellationToken cancellationToken)
    {
        await PublishAsync(StockSagaRoutingKeys.RecoverRequested, request, cancellationToken);
        return Accepted();
    }

    private async Task PublishAsync(string routingKey, StockSagaRequest request, CancellationToken cancellationToken)
    {
        var correlationId = !string.IsNullOrWhiteSpace(request.CorrelationId)
            ? request.CorrelationId
            : request.SaleId?.ToString(CultureInfo.InvariantCulture);

        await eventPublisher.PublishAsync(
            routingKey,
            new
            {
                request.ProductId,
                request.Quantity,
                request.ActorUserId,
                request.ActorUsername,
                request.SaleId,
                request.CorrelationId
            },
            correlationId);
    }

    public sealed record StockSagaRequest(long ProductId, int Quantity, long ActorUserId, string ActorUsername, long? SaleId, string? CorrelationId);
}
