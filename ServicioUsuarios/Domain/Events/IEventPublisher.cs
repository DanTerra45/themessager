namespace Domain.Events;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, object @event, string? correlationId = null);
}
