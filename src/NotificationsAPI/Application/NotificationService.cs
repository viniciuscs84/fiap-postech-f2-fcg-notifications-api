using NotificationsAPI.Integration;

namespace NotificationsAPI.Application;

public sealed class NotificationService(INotificationPresenter presenter)
{
    public Task HandleAsync(UserCreatedEvent message, CancellationToken cancellationToken)
    {
        ValidateRecipient(message.Email);

        if (message.Version != 2 || message.EventId == Guid.Empty || message.UserId == Guid.Empty)
        {
            throw new ArgumentException("EventId and UserId are required.", nameof(message));
        }

        return presenter.PresentWelcomeAsync(message.Email, cancellationToken);
    }

    public Task HandleAsync(PaymentProcessedEvent message, CancellationToken cancellationToken)
    {
        if (message.Version != 2 || message.EventId == Guid.Empty || message.OrderId == Guid.Empty ||
            message.UserId == Guid.Empty || message.GameId == Guid.Empty)
        {
            throw new ArgumentException("EventId, OrderId, UserId and GameId are required.", nameof(message));
        }

        if (string.Equals(message.Status, PaymentStatus.Rejected, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        if (!string.Equals(message.Status, PaymentStatus.Approved, StringComparison.Ordinal))
        {
            throw new ArgumentException("Payment status must be Approved or Rejected.", nameof(message));
        }

        ValidateRecipient(message.RecipientEmail);

        return presenter.PresentPurchaseApprovedAsync(message.RecipientEmail, message.GameId, cancellationToken);
    }

    private static void ValidateRecipient(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (email.IndexOfAny(['\r', '\n']) >= 0 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) ||
            !string.Equals(address.Address, email, StringComparison.Ordinal))
            throw new ArgumentException("The notification recipient email is invalid.", nameof(email));
    }
}
