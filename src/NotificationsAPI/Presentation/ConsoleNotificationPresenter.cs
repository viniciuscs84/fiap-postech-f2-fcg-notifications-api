using Microsoft.Extensions.Localization;
using NotificationsAPI.Application;

namespace NotificationsAPI.Presentation;

public sealed class ConsoleNotificationPresenter(
    IStringLocalizer<NotificationMessages> localizer) : INotificationPresenter
{
    public Task PresentWelcomeAsync(string recipientEmail, CancellationToken cancellationToken)
    {
        Console.WriteLine(localizer["WelcomeEmail", recipientEmail]);
        return Task.CompletedTask;
    }

    public Task PresentPurchaseApprovedAsync(string recipientEmail, Guid gameId, CancellationToken cancellationToken)
    {
        Console.WriteLine(localizer["PurchaseApprovedEmail", recipientEmail, gameId]);
        return Task.CompletedTask;
    }
}
