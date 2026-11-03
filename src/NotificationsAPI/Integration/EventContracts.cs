namespace NotificationsAPI.Integration;

public sealed record UserCreatedEvent(Guid EventId, Guid UserId, string Email, int Version = 2);

public sealed record PaymentProcessedEvent(Guid EventId, Guid OrderId, Guid UserId, Guid GameId, string Status, string RecipientEmail, int Version = 2);

public static class PaymentStatus
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
