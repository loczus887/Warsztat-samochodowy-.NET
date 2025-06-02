using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkshopManager.Services.Interfaces;

namespace WorkshopManager.BackgroundServices;

public class DailyReportBackgroundService : BackgroundService
{
    private readonly ILogger<DailyReportBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public DailyReportBackgroundService(
        ILogger<DailyReportBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Report Background Service is starting.");

        var interval = _configuration.GetValue<bool>("Development")
            ? TimeSpan.FromMinutes(2)  // Co 2 minuty w trybie deweloperskim
            : TimeSpan.FromHours(24);  // Raz dziennie w produkcji

        using PeriodicTimer timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await GenerateAndSendDailyReportAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Daily Report Background Service is stopping.");
        }
    }

    private async Task GenerateAndSendDailyReportAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Generating and sending daily report at: {time}", DateTimeOffset.Now);

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

                // Pobierz adres e-mail z konfiguracji
                var adminEmail = _configuration.GetValue<string>("AdminEmail") ?? "admin@workshop.com";

                // Wygeneruj i wyślij raport
                await reportService.SendDailyReportEmailAsync(adminEmail);

                _logger.LogInformation("Daily report sent successfully to {email}", adminEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating and sending daily report");
        }
    }
}