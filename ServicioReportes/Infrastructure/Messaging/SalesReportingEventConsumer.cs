using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServicioReportes.Infrastructure.Reports.Persistence;

namespace ServicioReportes.Infrastructure.Messaging;

public sealed class SalesReportingEventConsumer(IServiceScopeFactory scopeFactory, ILogger<SalesReportingEventConsumer> logger, IConfiguration configuration) : BackgroundService
{
    private const string RegisteredRoutingKey = "sales.registered";
    private const string CancelledRoutingKey = "sales.cancelled";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hostName = configuration["RabbitMQ:HostName"] ?? configuration["RabbitMQ:Host"] ?? "localhost";
        var userName = configuration["RabbitMQ:UserName"] ?? configuration["RabbitMQ:User"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        var exchange = configuration["RabbitMQ:Exchange"] ?? "saga.exchange";
        var queueName = configuration["RabbitMQ:Queue"] ?? "reports.sales.readmodel";

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
        _channel.QueueBind(queueName, exchange, RegisteredRoutingKey);
        _channel.QueueBind(queueName, exchange, CancelledRoutingKey);
        _channel.BasicQos(0, 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;
        _channel.BasicConsume(queueName, autoAck: false, consumer);

        logger.LogInformation("Consumiendo eventos de ventas para reportes desde RabbitMQ en la cola {QueueName}.", queueName);

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
            using var scope = scopeFactory.CreateScope();
            var projectionWriter = scope.ServiceProvider.GetRequiredService<SalesReportProjectionWriter>();

            switch (args.RoutingKey)
            {
                case RegisteredRoutingKey:
                    var registeredEvent = JsonSerializer.Deserialize<SaleRegisteredEvent>(payload, SerializerOptions)
                        ?? throw new InvalidOperationException("No se pudo deserializar el evento sales.registered.");

                    await projectionWriter.UpsertRegisteredSaleAsync(
                        new SalesReportingProjection(
                            registeredEvent.SaleId,
                            registeredEvent.Code,
                            DateOnly.FromDateTime(registeredEvent.CreatedAt),
                            registeredEvent.CreatedAt,
                            DateTime.Now,
                            registeredEvent.CustomerId,
                            registeredEvent.CustomerCiNit,
                            registeredEvent.CustomerBusinessName,
                            registeredEvent.ActorUserId,
                            registeredEvent.ActorUsername,
                            registeredEvent.Channel,
                            registeredEvent.PaymentMethod,
                            registeredEvent.Total,
                            registeredEvent.AmountInWords,
                            registeredEvent.ActorUserId,
                            registeredEvent.ActorUsername,
                            registeredEvent.ActorUserId,
                            registeredEvent.ActorUsername,
                            registeredEvent.ActorUserId,
                            registeredEvent.ActorUsername,
                            DateTime.Now,
                            registeredEvent.Lines.Select(line => new SalesReportingProjectionLine(line.ProductId, line.ProductName, line.LotCode, line.Quantity, line.UnitPrice, line.Subtotal)).ToList()),
                        CancellationToken.None);
                    break;

                case CancelledRoutingKey:
                    var cancelledEvent = JsonSerializer.Deserialize<SaleCancelledEvent>(payload, SerializerOptions)
                        ?? throw new InvalidOperationException("No se pudo deserializar el evento sales.cancelled.");

                    await projectionWriter.MarkSaleCancelledAsync(
                        new SaleCancelledProjection(cancelledEvent.SaleId, cancelledEvent.Reason, cancelledEvent.ActorUserId, cancelledEvent.ActorUsername, cancelledEvent.CancelledAt),
                        CancellationToken.None);
                    break;

                default:
                    throw new InvalidOperationException($"Routing key no soportada: {args.RoutingKey}");
            }

            _channel.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al procesar un evento de ventas para reportes.");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
