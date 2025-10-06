using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace CoreAxis.SharedKernel.Observability.HealthChecks;

public class OutboxLivenessHealthCheck : IHealthCheck
{
    private readonly Outbox.IOutboxRepository _outboxRepository;
    private readonly ILogger<OutboxLivenessHealthCheck> _logger;

    public OutboxLivenessHealthCheck(Outbox.IOutboxRepository outboxRepository, ILogger<OutboxLivenessHealthCheck> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // Minimal query to ensure repository connectivity/responsiveness
            await _outboxRepository.GetUnprocessedMessagesAsync(1, cts.Token);
            return HealthCheckResult.Healthy("Outbox repository responsive");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox health check failed");
            return HealthCheckResult.Unhealthy("Outbox repository not responsive", ex);
        }
    }
}