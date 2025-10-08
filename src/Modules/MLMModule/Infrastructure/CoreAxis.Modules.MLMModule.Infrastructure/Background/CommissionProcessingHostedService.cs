using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.MLMModule.Application.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Background;

public class CommissionProcessingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommissionProcessingHostedService> _logger;
    private readonly IConfiguration _configuration;

    public CommissionProcessingHostedService(
        IServiceProvider serviceProvider,
        ILogger<CommissionProcessingHostedService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int>(
            "MLMModule:CommissionProcessing:IntervalMinutes", 5);
        var batchSize = _configuration.GetValue<int>(
            "MLMModule:CommissionProcessing:BatchSize", 100);

        _logger.LogInformation("CommissionProcessingHostedService started with interval {Interval} minutes and batch size {BatchSize}", intervalMinutes, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new ProcessPendingCommissionsCommand
                {
                    ProcessedBy = nameof(CommissionProcessingHostedService),
                    BatchSize = batchSize,
                    Notes = "Scheduled processing"
                };

                var result = await mediator.Send(command, stoppingToken);
                var count = result?.Count ?? 0;
                _logger.LogInformation("Processed {Count} pending commissions in scheduled run", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during scheduled commission processing");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("CommissionProcessingHostedService stopped");
    }
}