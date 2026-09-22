using Qms.Infrastructure.Outbox;

namespace Qms.Worker;

public class Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("QMS background worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var escalated = await scope.ServiceProvider.GetRequiredService<DeadlineEscalationService>()
                    .EscalateAsync(stoppingToken);
                var processed = await scope.ServiceProvider.GetRequiredService<OutboxProcessor>()
                    .ProcessBatchAsync(stoppingToken);
                logger.LogInformation("QMS worker cycle completed: {Escalated} tasks escalated, {Processed} messages processed",
                    escalated, processed);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "QMS worker cycle failed; processing will retry");
            }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
