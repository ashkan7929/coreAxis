namespace CoreAxis.SharedKernel.Outbox;

/// <summary>
/// Options for configuring the Outbox publisher behavior.
/// </summary>
public class OutboxOptions
{
    /// <summary>
    /// Polling interval in seconds for the OutboxPublisher background service.
    /// Default is 30 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 30;
}