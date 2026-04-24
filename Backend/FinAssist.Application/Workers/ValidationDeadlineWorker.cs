using FinAssist.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinAssist.Application.Workers;

public class ValidationDeadlineWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ValidationDeadlineWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[DeadlineWorker] Démarré");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ValidationDeadlineService>();
                await service.ProcessDeadlinesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DeadlineWorker] Erreur lors du traitement");
            }

            // 1 min en dev, 1h en prod
            var interval = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? TimeSpan.FromMinutes(1)
                : TimeSpan.FromHours(1);
            await Task.Delay(interval, stoppingToken);
        }
    }
}
