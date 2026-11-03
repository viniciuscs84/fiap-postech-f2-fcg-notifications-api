namespace NotificationsAPI.Integration;

public static class EventTopology
{
    public const string UserCreatedRoutingKey = "user.created";
    public const string PaymentProcessedRoutingKey = "payment.processed";
    public const string UserCreatedQueue = "notifications.user-created";
    public const string PaymentProcessedQueue = "notifications.payment-processed";
    public const string UserCreatedDeadQueue = "notifications.user-created.dead";
    public const string PaymentProcessedDeadQueue = "notifications.payment-processed.dead";
}
