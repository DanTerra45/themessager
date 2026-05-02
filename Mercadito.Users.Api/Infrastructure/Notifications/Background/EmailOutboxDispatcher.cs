using Mercadito.Users.Api.Application.Notifications.Exceptions;
using Mercadito.Users.Api.Application.Notifications.Models;
using Mercadito.Users.Api.Application.Notifications.Ports.Output;
using Mercadito.Users.Api.Infrastructure.Notifications.Options;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Mercadito.Users.Api.Infrastructure.Notifications.Background
{
    public sealed class EmailOutboxDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly EmailOutboxOptions _options;
        private readonly ILogger<EmailOutboxDispatcher> _logger;

        public EmailOutboxDispatcher(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<EmailOutboxOptions> options,
            ILogger<EmailOutboxDispatcher> logger)
        {
            ArgumentNullException.ThrowIfNull(serviceScopeFactory);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _serviceScopeFactory = serviceScopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DispatchBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (MySqlException exception)
                {
                    _logger.LogError(exception, "Error al procesar la cola de correos.");
                }
                catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
                {
                    _logger.LogError(exception, "Error al procesar la cola de correos.");
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
            }
        }

        private async Task DispatchBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var emailOutboxRepository = scope.ServiceProvider.GetRequiredService<IEmailOutboxRepository>();

            try
            {
                emailSender.EnsureAvailable();
            }
            catch (EmailDeliveryException exception)
            {
                _logger.LogWarning(exception, "La cola de correos está activa, pero SMTP no está disponible.");
                return;
            }

            var currentUtc = DateTime.UtcNow;
            var batchSize = Math.Max(1, _options.BatchSize);
            var messages = await emailOutboxRepository.ReservePendingBatchAsync(batchSize, currentUtc, cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    var toName = string.Empty;
                    if (message.ToName != null)
                    {
                        toName = message.ToName;
                    }

                    var htmlBody = string.Empty;
                    if (message.HtmlBody != null)
                    {
                        htmlBody = message.HtmlBody;
                    }

                    await emailSender.SendAsync(
                        new EmailMessage
                        {
                            ToAddress = message.ToAddress,
                            ToName = toName,
                            Subject = message.Subject,
                            PlainTextBody = message.PlainTextBody,
                            HtmlBody = htmlBody
                        },
                        cancellationToken);

                    await emailOutboxRepository.MarkSentAsync(message.Id, DateTime.UtcNow, cancellationToken);
                }
                catch (EmailDeliveryException exception)
                {
                    await HandleDeliveryFailureAsync(emailOutboxRepository, message, exception.Message, cancellationToken);
                }
            }
        }

        private async Task HandleDeliveryFailureAsync(IEmailOutboxRepository emailOutboxRepository, EmailOutboxItem message, string errorMessage, CancellationToken cancellationToken)
        {
            if (message.Attempts >= Math.Max(1, _options.MaxAttempts))
            {
                await emailOutboxRepository.MarkExhaustedAsync(message.Id, DateTime.UtcNow, errorMessage, cancellationToken);
                return;
            }

            var delaySeconds = Math.Max(1, _options.BaseRetryDelaySeconds) * Math.Max(1, message.Attempts);
            var nextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
            await emailOutboxRepository.MarkForRetryAsync(message.Id, nextAttemptAtUtc, errorMessage, cancellationToken);
        }
    }
}
