using System.Text.Json;

namespace NotificationsAPI.Messaging;

public sealed class EventMetrics
{
    private long consumeFailures;
    public long ConsumeFailures => Interlocked.Read(ref consumeFailures);
    public void RecordConsumeFailure() => Interlocked.Increment(ref consumeFailures);
}

public sealed class EventMetricsWorker(EventMetrics metrics, ILogger<EventMetricsWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = JsonSerializer.Serialize(new
                {
                    service = "NotificationsAPI",
                    fcg_event_consume_failures_total = metrics.ConsumeFailures
                });
                var path = Path.Combine(Path.GetTempPath(), "fcg-metrics.json");
                await File.WriteAllTextAsync(path + ".tmp", snapshot, stoppingToken);
                File.Move(path + ".tmp", path, true);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not update internal event metrics snapshot.");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
