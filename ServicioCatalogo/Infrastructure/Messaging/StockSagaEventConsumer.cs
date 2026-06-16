using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServicioCatalogo.Application.Products.Ports.Input;
using ServicioCatalogo.Domain.Shared;

namespace ServicioCatalogo.Infrastructure.Messaging;

public sealed class StockSagaEventConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockSagaEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public StockSagaEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<StockSagaEventConsumer> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hostName = ResolveSetting("RabbitMQ:HostName", "RabbitMQ:Host") ?? "localhost";
        var userName = ResolveSetting("RabbitMQ:UserName", "RabbitMQ:User") ?? "guest";
        var password = ResolveSetting("RabbitMQ:Password") ?? "guest";
        var exchange = ResolveSetting("RabbitMQ:Exchange") ?? "saga.exchange";
        var queueName = ResolveSetting("RabbitMQ:Queue") ?? "catalog.stock.saga";

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
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.ReserveRequested);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.RecoverRequested);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.ReserveRequestedLegacy);
        _channel.QueueBind(queueName, exchange, StockSagaRoutingKeys.RecoverRequestedLegacy);
        _channel.BasicQos(0, 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("Consumiendo eventos de saga de stock desde RabbitMQ en la cola {QueueName}.", queueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(args.Body.ToArray());
            var routingKey = args.RoutingKey;
            var message = JsonSerializer.Deserialize<StockSagaCommandEvent>(payload, SerializerOptions);
            if (message is null)
            {
                _logger.LogWarning("Se descartó un evento de saga de stock inválido con routing key {RoutingKey}.", routingKey);
                _channel.BasicAck(args.DeliveryTag, multiple: false);
                return;
            }

            var correlationId = args.BasicProperties?.CorrelationId;
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = message.CorrelationId;
            }

            using var scope = _scopeFactory.CreateScope();
            var productUseCase = scope.ServiceProvider.GetRequiredService<IProductManagementUseCase>();

            Result result = routingKey switch
            {
                StockSagaRoutingKeys.ReserveRequested or StockSagaRoutingKeys.ReserveRequestedLegacy
                    => await productUseCase.ReserveStockAsync(message.ProductId, message.Quantity, message.ActorUserId, CancellationToken.None),
                StockSagaRoutingKeys.RecoverRequested or StockSagaRoutingKeys.RecoverRequestedLegacy
                    => await productUseCase.RecoverStockAsync(message.ProductId, message.Quantity, message.ActorUserId, CancellationToken.None),
                _ => Result.Failure($"Evento no soportado: {routingKey}")
            };

            if (result.IsFailure)
            {
                _logger.LogWarning("La saga de stock falló para {RoutingKey}: {Error}", routingKey, result.ErrorMessage);
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                PublishResult(routingKey, correlationId, message, result);
            }

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error al procesar un evento de saga de stock.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }

    private void PublishResult(string requestRoutingKey, string correlationId, StockSagaCommandEvent command, Result result)
    {
        if (_channel is null)
        {
            return;
        }

        var responseRoutingKey = requestRoutingKey switch
        {
            StockSagaRoutingKeys.ReserveRequested or StockSagaRoutingKeys.ReserveRequestedLegacy
                => result.IsSuccess ? StockSagaRoutingKeys.ReserveSucceeded : StockSagaRoutingKeys.ReserveFailed,
            StockSagaRoutingKeys.RecoverRequested or StockSagaRoutingKeys.RecoverRequestedLegacy
                => result.IsSuccess ? StockSagaRoutingKeys.RecoverSucceeded : StockSagaRoutingKeys.RecoverFailed,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(responseRoutingKey))
        {
            return;
        }

        var response = new StockSagaResultEvent(
            command.ProductId,
            command.Quantity,
            command.ActorUserId,
            command.ActorUsername,
            command.SaleId,
            correlationId,
            result.IsSuccess,
            result.IsFailure && result.Errors.Count > 0,
            result.IsFailure ? result.ErrorMessage : null);

        var properties = _channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.ContentType = "application/json";
        properties.CorrelationId = correlationId;

        var exchange = ResolveSetting("RabbitMQ:Exchange") ?? "saga.exchange";
        var responsePayload = JsonSerializer.SerializeToUtf8Bytes(response);
        _channel.BasicPublish(exchange, responseRoutingKey, properties, responsePayload);
    }

    private string? ResolveSetting(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
