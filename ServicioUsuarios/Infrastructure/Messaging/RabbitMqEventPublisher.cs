using Domain.Events;
using RabbitMQ.Client;
using System.Text.Json;

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

    public RabbitMqEventPublisher(IConfiguration configuration)
    {
        var hostName = ResolveSetting(configuration, "RabbitMQ:HostName", "RabbitMQ:Host") ?? "localhost";
        var userName = ResolveSetting(configuration, "RabbitMQ:UserName", "RabbitMQ:User") ?? "guest";
        var password = ResolveSetting(configuration, "RabbitMQ:Password") ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = true
        };

        _exchange = ResolveSetting(configuration, "RabbitMQ:Exchange") ?? "saga.exchange";
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync(string routingKey, object @event, string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentNullException.ThrowIfNull(@event);

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, SerializerOptions);
        var properties = _channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.ContentType = "application/json";

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            properties.CorrelationId = correlationId;
        }

        _channel.BasicPublish(_exchange, routingKey, properties, payload);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
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
