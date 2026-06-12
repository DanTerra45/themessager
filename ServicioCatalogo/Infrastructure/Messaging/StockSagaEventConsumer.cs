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
    private const string ReserveRoutingKey = "sales.stock.reserved";
    private const string RecoverRoutingKey = "sales.stock.recovered";

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
        var hostName = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost";
        var userName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:User"] ?? "guest";
        var password = _configuration["RabbitMQ:Password"] ?? "guest";
        var exchange = _configuration["RabbitMQ:Exchange"] ?? "saga.exchange";
        var queueName = _configuration["RabbitMQ:Queue"] ?? "catalog.stock.saga";

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
        _channel.QueueBind(queueName, exchange, ReserveRoutingKey);
        _channel.QueueBind(queueName, exchange, RecoverRoutingKey);
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

            using var scope = _scopeFactory.CreateScope();
            var productUseCase = scope.ServiceProvider.GetRequiredService<IProductManagementUseCase>();

            Result result = routingKey switch
            {
                ReserveRoutingKey => await HandleReserveAsync(productUseCase, payload, CancellationToken.None),
                RecoverRoutingKey => await HandleRecoverAsync(productUseCase, payload, CancellationToken.None),
                _ => Result.Failure($"Evento no soportado: {routingKey}")
            };

            if (result.IsFailure)
            {
                _logger.LogWarning("La saga de stock falló para {RoutingKey}: {Error}", routingKey, result.ErrorMessage);
            }

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error al procesar un evento de saga de stock.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task<Result> HandleReserveAsync(IProductManagementUseCase productUseCase, string payload, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<SaleStockReservedEvent>(payload, SerializerOptions);
        if (message is null)
        {
            return Result.Failure("No se pudo leer el evento de reserva de stock.");
        }

        return await productUseCase.ReserveStockAsync(message.ProductId, message.Quantity, message.ActorUserId, cancellationToken);
    }

    private async Task<Result> HandleRecoverAsync(IProductManagementUseCase productUseCase, string payload, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<SaleStockRecoveredEvent>(payload, SerializerOptions);
        if (message is null)
        {
            return Result.Failure("No se pudo leer el evento de recuperación de stock.");
        }

        return await productUseCase.RecoverStockAsync(message.ProductId, message.Quantity, message.ActorUserId, cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}