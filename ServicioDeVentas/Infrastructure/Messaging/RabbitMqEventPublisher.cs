using Domain.Events;
using System.Text.Json;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchange;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public RabbitMqEventPublisher(IConfiguration cfg)
    {
        var hostName = ResolveSetting(cfg, "RabbitMQ:HostName", "RabbitMQ:Host") ?? "localhost";
        var userName = ResolveSetting(cfg, "RabbitMQ:UserName", "RabbitMQ:User") ?? "guest";
        var password = ResolveSetting(cfg, "RabbitMQ:Password") ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = true
        };

        _exchange = ResolveSetting(cfg, "RabbitMQ:Exchange") ?? "saga.exchange";
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
    }

    public async Task PublishAsync(string routingKey, object @event, string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentNullException.ThrowIfNull(@event);

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, SerializerOptions);

        var props = _channel.CreateBasicProperties();
        props.DeliveryMode = 2;
        props.ContentType = "application/json";

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            props.CorrelationId = correlationId;
        }

        await _publishLock.WaitAsync();
        try
        {
            _channel.BasicPublish(_exchange, routingKey, props, payload);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public void Dispose()
    {
        _publishLock.Dispose();
        _channel?.Close();
        _connection?.Close();
    }

    private static string? ResolveSetting(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
