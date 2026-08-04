using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class AttributeEvidenceRoutingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttributeEvidenceRoutingWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public AttributeEvidenceRoutingWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AttributeEvidenceRoutingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var minutes = configuration.GetValue<int?>(
            "AttributeRouting:PollIntervalMinutes") ?? 5;

        _pollInterval = TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(_pollInterval);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IAttributeSubmissionService>();

                await service.RouteExpiredEvidenceSubmissionsAsync(
                    "Genesis Attribute Routing Worker");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attribute evidence routing cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
