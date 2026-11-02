using System.Text.Json;
using NotificationsAPI.Application;

namespace NotificationsAPI.Integration;

public sealed class NotificationEventDispatcher(NotificationService notificationService)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    public Task DispatchAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken cancellationToken) =>
        routingKey switch
        {
            EventTopology.UserCreatedRoutingKey => notificationService.HandleAsync(
                Deserialize<UserCreatedEvent>(body), cancellationToken),
            EventTopology.PaymentProcessedRoutingKey => notificationService.HandleAsync(
                Deserialize<PaymentProcessedEvent>(body), cancellationToken),
            _ => throw new ArgumentException($"Unsupported routing key: {routingKey}", nameof(routingKey))
        };

    private static T Deserialize<T>(ReadOnlyMemory<byte> body) =>
        JsonSerializer.Deserialize<T>(body.Span, SerializerOptions)
        ?? throw new JsonException($"The {typeof(T).Name} payload is empty.");
}
