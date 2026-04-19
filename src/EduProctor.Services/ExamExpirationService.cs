
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EduProctor.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace EduProctor.Services;

public class ExamExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExamExpirationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public ExamExpirationService(
        IServiceProvider serviceProvider,
        ILogger<ExamExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var examService = scope.ServiceProvider.GetRequiredService<IExamService>();

                await examService.AutoSubmitExpiredExamsAsync();
                _logger.LogInformation("Expired exams checked at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired exams");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}