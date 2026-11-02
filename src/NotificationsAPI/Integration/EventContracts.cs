namespace NotificationsAPI.Integration;

public sealed record UserCreatedEvent(Guid UserId, string Email);

public sealed record PaymentProcessedEvent(Guid UserId, Guid GameId, string Status, string RecipientEmail);

public static class PaymentStatus
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
