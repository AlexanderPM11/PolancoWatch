using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Domain.Entities;
using PolancoWatch.Domain.Common;
using PolancoWatch.Infrastructure.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System;

namespace PolancoWatch.Infrastructure.Services;

public class WebMonitorHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebMonitorHostedService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebMonitorHostedService(
        IServiceProvider serviceProvider,
        ILogger<WebMonitorHostedService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Web Monitor Hosted Service is starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PerformChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while performing web monitor checks.");
            }
        }
    }

    private async Task PerformChecksAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

        var monitors = await context.WebMonitors
            .Where(m => m.IsActive)
            .ToListAsync(ct);

        var settings = await context.NotificationSettings.FirstOrDefaultAsync(ct);

        var httpClient = _httpClientFactory.CreateClient("WebMonitor");
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        foreach (var monitor in monitors)
        {
            // Simple interval logic: if LastCheck + Interval < Now, skip
            if (monitor.LastCheckTime.HasValue && 
                monitor.LastCheckTime.Value.AddSeconds(monitor.CheckIntervalSeconds) > TimeHelper.Now)
            {
                continue;
            }

            var check = new WebCheck
            {
                WebMonitorId = monitor.Id,
                Timestamp = TimeHelper.Now
            };

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await httpClient.GetAsync(monitor.Url, ct);
                sw.Stop();

                check.IsUp = response.IsSuccessStatusCode;
                check.StatusCode = (int)response.StatusCode;
                check.LatencyMs = sw.Elapsed.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                sw.Stop();
                check.IsUp = false;
                check.StatusCode = 0;
                check.LatencyMs = sw.Elapsed.TotalMilliseconds;
                check.ErrorMessage = ex.Message;
            }

            // Detect Status Change (UP -> DOWN)
            if (monitor.LastStatusUp && !check.IsUp)
            {
                await SendFailureAlert(monitor, check, telegramService, settings);
            }
            // Optional: UP -> DOWN Recovery
            else if (!monitor.LastStatusUp && check.IsUp)
            {
                await SendRecoveryAlert(monitor, check, telegramService, settings);
            }

            // Update Monitor State
            monitor.LastCheckTime = TimeHelper.Now;
            monitor.LastStatusUp = check.IsUp;
            monitor.LastLatencyMs = check.LatencyMs;

            context.WebChecks.Add(check);
            
            // Prune old checks (keep last 1000 per monitor or similar)
            // For now, just save.
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task SendFailureAlert(WebMonitor monitor, WebCheck check, ITelegramService telegram, NotificationSettings? settings)
    {
        var message = $"🔴 *Web Monitor Alert*\n\n" +
                      $"*Application Name:* {monitor.Name}\n" +
                      $"*URL:* {monitor.Url}\n" +
                      $"*Status:* DOWN ({(check.StatusCode > 0 ? check.StatusCode.ToString() : "Timeout/Error")})\n" +
                      $"*Error:* {check.ErrorMessage ?? "None"}\n" +
                      $"*Time:* {TimeHelper.Now:yyyy-MM-dd HH:mm:ss} (AST)";

        await telegram.SendMessageAsync(message, settings);
    }

    private async Task SendRecoveryAlert(WebMonitor monitor, WebCheck check, ITelegramService telegram, NotificationSettings? settings)
    {
        var message = $"🟢 *Web Monitor Recovered*\n\n" +
                      $"*Application Name:* {monitor.Name}\n" +
                      $"*URL:* {monitor.Url}\n" +
                      $"*Status:* UP ({check.StatusCode})\n" +
                      $"*Latency:* {check.LatencyMs:F0}ms\n" +
                      $"*Time:* {TimeHelper.Now:yyyy-MM-dd HH:mm:ss} (AST)";

        await telegram.SendMessageAsync(message, settings);
    }
}
