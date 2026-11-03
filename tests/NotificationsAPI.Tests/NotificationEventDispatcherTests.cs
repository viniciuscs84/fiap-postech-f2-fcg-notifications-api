using System.Text;
using NotificationsAPI.Application;
using NotificationsAPI.Integration;

namespace NotificationsAPI.Tests;

public sealed class NotificationEventDispatcherTests
{
    [Fact]
    public async Task UserCreatedEventProducesWelcomeNotification()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));
        var userId = Guid.NewGuid();

        await dispatcher.DispatchAsync(
            EventTopology.UserCreatedRoutingKey,
            Json($$"""{"EventId":"{{Guid.NewGuid()}}","UserId":"{{userId}}","Email":"pessoa@example.com"}"""),
            CancellationToken.None);

        Assert.Equal(["welcome:pessoa@example.com"], presenter.Messages);
    }

    [Fact]
    public async Task ApprovedPaymentProducesPurchaseConfirmation()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        await dispatcher.DispatchAsync(
            EventTopology.PaymentProcessedRoutingKey,
            Json($$"""{"EventId":"{{Guid.NewGuid()}}","OrderId":"{{Guid.NewGuid()}}","UserId":"{{userId}}","GameId":"{{gameId}}","Status":"Approved","RecipientEmail":"pessoa@example.com"}"""),
            CancellationToken.None);

        Assert.Equal([$"purchase:pessoa@example.com:{gameId}"], presenter.Messages);
    }

    [Fact]
    public async Task RejectedPaymentDoesNotProducePurchaseConfirmation()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));

        await dispatcher.DispatchAsync(
            EventTopology.PaymentProcessedRoutingKey,
            Json($$"""{"EventId":"{{Guid.NewGuid()}}","OrderId":"{{Guid.NewGuid()}}","UserId":"{{Guid.NewGuid()}}","GameId":"{{Guid.NewGuid()}}","Status":"Rejected","RecipientEmail":"pessoa@example.com"}"""),
            CancellationToken.None);

        Assert.Empty(presenter.Messages);
    }

    [Fact]
    public async Task WelcomeContractDoesNotRequireCredentialsOrTokens()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));

        await dispatcher.DispatchAsync(
            EventTopology.UserCreatedRoutingKey,
            Json($$"""{"EventId":"{{Guid.NewGuid()}}","UserId":"{{Guid.NewGuid()}}","Email":"pessoa@example.com"}"""),
            CancellationToken.None);

        var message = Assert.Single(presenter.Messages);
        Assert.DoesNotContain("password", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MalformedRecipientDoesNotReachConsolePresenter()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));
        var payload = $$"""{"EventId":"{{Guid.NewGuid()}}","UserId":"{{Guid.NewGuid()}}","Email":"recipient@example.com\nInjected log line"}""";

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
            EventTopology.UserCreatedRoutingKey, Json(payload), CancellationToken.None));
        Assert.Empty(presenter.Messages);
    }

    [Fact]
    public async Task MissingEventIdentityDoesNotReachConsolePresenter()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
            EventTopology.UserCreatedRoutingKey,
            Json($$"""{"UserId":"{{Guid.NewGuid()}}","Email":"pessoa@example.com"}"""),
            CancellationToken.None));

        Assert.Empty(presenter.Messages);
    }

    [Fact]
    public async Task MissingPurchaseAttemptDoesNotReachConsolePresenter()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
            EventTopology.PaymentProcessedRoutingKey,
            Json($$"""{"EventId":"{{Guid.NewGuid()}}","UserId":"{{Guid.NewGuid()}}","GameId":"{{Guid.NewGuid()}}","Status":"Approved","RecipientEmail":"pessoa@example.com"}"""),
            CancellationToken.None));

        Assert.Empty(presenter.Messages);
    }

    [Fact]
    public async Task UnsupportedContractVersionDoesNotReachConsolePresenter()
    {
        var presenter = new RecordingNotificationPresenter();
        var dispatcher = new NotificationEventDispatcher(new NotificationService(presenter));

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
            EventTopology.UserCreatedRoutingKey,
            Json($$"""{"EventId":"{{Guid.NewGuid()}}","UserId":"{{Guid.NewGuid()}}","Email":"pessoa@example.com","Version":3}"""),
            CancellationToken.None));

        Assert.Empty(presenter.Messages);
    }

    private static ReadOnlyMemory<byte> Json(string value) => Encoding.UTF8.GetBytes(value);

    private sealed class RecordingNotificationPresenter : INotificationPresenter
    {
        public List<string> Messages { get; } = [];

        public Task PresentWelcomeAsync(string recipientEmail, CancellationToken cancellationToken)
        {
            Messages.Add($"welcome:{recipientEmail}");
            return Task.CompletedTask;
        }

        public Task PresentPurchaseApprovedAsync(string recipientEmail, Guid gameId, CancellationToken cancellationToken)
        {
            Messages.Add($"purchase:{recipientEmail}:{gameId}");
            return Task.CompletedTask;
        }
    }
}
