using System.Text;
using System.Text.Json;
using Application.Sagas;
using Domain.Events;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Messaging;

public sealed class StockSagaResponseConsumer(
    IConfiguration configuration,
    ILogger<StockSagaResponseConsumer> logger,
    IStockSagaCoordinator stockSagaCoordinator) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hostName = ResolveSetting(configuration, "RabbitMQ:HostName", "RabbitMQ:Host") ?? "localhost";
        var userName = ResolveSetting(configuration, "RabbitMQ:UserName", "RabbitMQ:User") ?? "guest";
        var password = ResolveSetting(configuration, "RabbitMQ:Password") ?? "guest";
        var exchange = ResolveSetting(configuration, "RabbitMQ:Exchange") ?? "saga.exchange";
        var queueName = ResolveSetting(configuration, "RabbitMQ:ResponseQueue") ?? "sales.stock.results";

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.ReserveSucceeded);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.ReserveFailed);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.RecoverSucceeded);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.RecoverFailed);
        _channel.BasicQos(0, 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        logger.LogInformation("Consumiendo respuestas de saga de stock desde RabbitMQ en la cola {QueueName}.", queueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var resultEvent = JsonSerializer.Deserialize<StockSagaResultEvent>(payload, SerializerOptions);
            var correlationId = args.BasicProperties?.CorrelationId;
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = resultEvent?.CorrelationId;
            }

            if (resultEvent is null || string.IsNullOrWhiteSpace(correlationId))
            {
                logger.LogWarning("Se descartó una respuesta de saga de stock inválida con routing key {RoutingKey}.", args.RoutingKey);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return Task.CompletedTask;
            }

            var handled = stockSagaCoordinator.TryComplete(new StockSagaCompletion(
                correlationId,
                args.RoutingKey,
                resultEvent.Success,
                resultEvent.IsBusinessFailure,
                resultEvent.ErrorMessage));

            if (!handled)
            {
                logger.LogWarning(
                    "No se encontró una saga pendiente para la correlación {CorrelationId} y routing key {RoutingKey}.",
                    correlationId,
                    args.RoutingKey);
            }

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al procesar una respuesta de saga de stock.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
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
