namespace NotificationsAPI.Application;

public interface INotificationPresenter
{
    Task PresentWelcomeAsync(string recipientEmail, CancellationToken cancellationToken);

    Task PresentPurchaseApprovedAsync(string recipientEmail, Guid gameId, CancellationToken cancellationToken);
}
