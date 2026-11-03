using NotificationsAPI.Messaging;

namespace NotificationsAPI.Tests;

public sealed class NotificationConsumerStateTests
{
    [Fact]
    public void Readiness_RequiresBothSubscriptionsAndResetsOnDisconnect()
    {
        var state = new NotificationConsumerState();
        Assert.False(state.IsReady);
        state.SetUserCreated(true);
        Assert.False(state.IsReady);
        state.SetPaymentProcessed(true);
        Assert.True(state.IsReady);
        state.Reset();
        Assert.False(state.IsReady);
    }
}
