using Microsoft.Extensions.Logging;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Domain.Entities;
using System.Text;
using System.Text.Json;

namespace PolancoWatch.Infrastructure.Services;

public class TelegramAlertNotifier : IAlertNotifier
{
    private readonly ILogger<TelegramAlertNotifier> _logger;
    private readonly HttpClient _httpClient;

    public TelegramAlertNotifier(ILogger<TelegramAlertNotifier> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task NotifyAsync(AlertRule rule, string message, double currentValue, NotificationSettings settings)
    {
        if (!settings.TelegramEnabled || string.IsNullOrEmpty(settings.TelegramBotToken) || string.IsNullOrEmpty(settings.TelegramChatId))
        {
            return;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{settings.TelegramBotToken}/sendMessage";
            var payload = new
            {
                chat_id = settings.TelegramChatId,
                text = $"🚨 *PolancoWatch Alert*\n\n{message}\n\n*Metric:* {rule.MetricType}\n*Value:* {currentValue}%\n*Threshold:* {rule.Threshold}%\n*Date:* {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                parse_mode = "Markdown"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send Telegram notification. Status: {Status}, Error: {Error}", response.StatusCode, error);
            }
            else
            {
                _logger.LogInformation("Telegram notification sent successfully for rule {RuleId}", rule.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Telegram notification for rule {RuleId}", rule.Id);
        }
    }
}
