using NotificationsAPI.Integration;

namespace NotificationsAPI.Application;

public sealed class NotificationService(INotificationPresenter presenter)
{
    public Task HandleAsync(UserCreatedEvent message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Email);

        if (message.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(message));
        }

        return presenter.PresentWelcomeAsync(message.Email, cancellationToken);
    }

    public Task HandleAsync(PaymentProcessedEvent message, CancellationToken cancellationToken)
    {
        if (message.Status.Equals(PaymentStatus.Rejected, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        if (!message.Status.Equals(PaymentStatus.Approved, StringComparison.Ordinal))
        {
            throw new ArgumentException("Payment status must be Approved or Rejected.", nameof(message));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message.RecipientEmail);

        if (message.UserId == Guid.Empty || message.GameId == Guid.Empty)
        {
            throw new ArgumentException("UserId and GameId are required.", nameof(message));
        }

        return presenter.PresentPurchaseApprovedAsync(message.RecipientEmail, message.GameId, cancellationToken);
    }
}
