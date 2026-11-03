using Microsoft.Extensions.Options;
using NotificationsAPI.Integration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationsAPI.Messaging;

public sealed class RabbitMqNotificationWorker(
    IOptions<RabbitMqOptions> options,
    NotificationEventDispatcher dispatcher,
    NotificationConsumerState state,
    EventMetrics metrics,
    ILogger<RabbitMqNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan[] ProcessingRetryDelays =
    [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)];
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
                state.Reset();
                logger.LogWarning(exception, "RabbitMQ notification consumer is unavailable; retrying in {Delay}.", ConnectionRetryDelay);
                await Task.Delay(ConnectionRetryDelay, stoppingToken);
            }
            finally { state.Reset(); }
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
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true), stoppingToken);

        await channel.ExchangeDeclareAsync(
            settings.Exchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: stoppingToken);

        await DeclareConsumerAsync(
            channel, EventTopology.UserCreatedQueue, EventTopology.UserCreatedDeadQueue,
            EventTopology.UserCreatedRoutingKey, stoppingToken);
        await DeclareConsumerAsync(
            channel, EventTopology.PaymentProcessedQueue, EventTopology.PaymentProcessedDeadQueue,
            EventTopology.PaymentProcessedRoutingKey, stoppingToken);

        logger.LogInformation("RabbitMQ notification consumers started on exchange {Exchange}.", settings.Exchange);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            if (!connection.IsOpen || !channel.IsOpen)
                throw new InvalidOperationException("Notification consumer subscription was lost.");
        }
    }

    private async Task DeclareConsumerAsync(
        IChannel channel,
        string queueName,
        string deadQueueName,
        string routingKey,
        CancellationToken stoppingToken)
    {
        await channel.QueueDeclareAsync(
            queueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            queueName, settings.Exchange, routingKey, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            deadQueueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            for (var attempt = 0; attempt <= ProcessingRetryDelays.Length; attempt++)
            {
                try
                {
                    await dispatcher.DispatchAsync(delivery.RoutingKey, delivery.Body.ToArray(), stoppingToken);
                    await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (attempt < ProcessingRetryDelays.Length &&
                    exception is not (ArgumentException or System.Text.Json.JsonException))
                {
                    metrics.RecordConsumeFailure();
                    logger.LogWarning(exception,
                        "Notification processing failed for {RoutingKey}, event {EventId}; retry {Attempt}.",
                        delivery.RoutingKey, delivery.BasicProperties.MessageId, attempt + 1);
                    await Task.Delay(ProcessingRetryDelays[attempt], stoppingToken);
                }
                catch (Exception exception)
                {
                    metrics.RecordConsumeFailure();
                    logger.LogError(exception,
                        "Notification processing failed for {RoutingKey}, event {EventId}; moving to {DeadQueue}.",
                        delivery.RoutingKey, delivery.BasicProperties.MessageId, deadQueueName);
                    try
                    {
                        var properties = new BasicProperties
                        {
                            ContentType = "application/json",
                            DeliveryMode = DeliveryModes.Persistent,
                            Type = delivery.BasicProperties.Type,
                            MessageId = delivery.BasicProperties.MessageId,
                            CorrelationId = delivery.BasicProperties.CorrelationId,
                            Headers = delivery.BasicProperties.Headers
                        };
                        await channel.BasicPublishAsync("", deadQueueName, mandatory: true,
                            basicProperties: properties, body: delivery.Body,
                            cancellationToken: stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
                    }
                    catch (Exception publishException)
                    {
                        logger.LogError(publishException,
                            "Could not preserve failed notification in {DeadQueue}; requeueing.",
                            deadQueueName);
                        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false,
                            requeue: true, stoppingToken);
                    }
                    return;
                }
            }
        };

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
        if (queueName == EventTopology.UserCreatedQueue) state.SetUserCreated(true);
        if (queueName == EventTopology.PaymentProcessedQueue) state.SetPaymentProcessed(true);
    }
}
