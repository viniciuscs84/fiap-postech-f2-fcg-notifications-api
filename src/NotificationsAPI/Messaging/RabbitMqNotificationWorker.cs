using Microsoft.Extensions.Options;
using NotificationsAPI.Integration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationsAPI.Messaging;

public sealed class RabbitMqNotificationWorker(
    IOptions<RabbitMqOptions> options,
    NotificationEventDispatcher dispatcher,
    ILogger<RabbitMqNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(5);
    private readonly RabbitMqOptions settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "RabbitMQ notification consumer is unavailable; retrying in {Delay}.", ConnectionRetryDelay);
                await Task.Delay(ConnectionRetryDelay, stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            settings.Exchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: stoppingToken);

        await DeclareConsumerAsync(
            channel, EventTopology.UserCreatedQueue, EventTopology.UserCreatedRoutingKey, stoppingToken);
        await DeclareConsumerAsync(
            channel, EventTopology.PaymentProcessedQueue, EventTopology.PaymentProcessedRoutingKey, stoppingToken);

        logger.LogInformation("RabbitMQ notification consumers started on exchange {Exchange}.", settings.Exchange);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task DeclareConsumerAsync(
        IChannel channel,
        string queueName,
        string routingKey,
        CancellationToken stoppingToken)
    {
        await channel.QueueDeclareAsync(
            queueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            queueName, settings.Exchange, routingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                await dispatcher.DispatchAsync(delivery.RoutingKey, delivery.Body.ToArray(), stoppingToken);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification event processing failed for routing key {RoutingKey}.", delivery.RoutingKey);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
    }
}
