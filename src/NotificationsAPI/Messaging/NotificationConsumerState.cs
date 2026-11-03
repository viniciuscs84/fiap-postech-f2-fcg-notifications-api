namespace NotificationsAPI.Messaging;

public sealed class NotificationConsumerState
{
    private int userCreated;
    private int paymentProcessed;

    public bool IsReady => Volatile.Read(ref userCreated) == 1 &&
                           Volatile.Read(ref paymentProcessed) == 1;

    public void SetUserCreated(bool value) => Interlocked.Exchange(ref userCreated, value ? 1 : 0);
    public void SetPaymentProcessed(bool value) => Interlocked.Exchange(ref paymentProcessed, value ? 1 : 0);

    public void Reset()
    {
        SetUserCreated(false);
        SetPaymentProcessed(false);
    }
}
