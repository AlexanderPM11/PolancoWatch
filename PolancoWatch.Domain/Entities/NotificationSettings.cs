using System;

namespace PolancoWatch.Domain.Entities;

public class NotificationSettings
{
    public int Id { get; set; }
    
    // Email
    public bool EmailEnabled { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string? SmtpUser { get; set; }
    public string? SmtpPass { get; set; }
    public bool SmtpEnableSsl { get; set; }
    public string? FromEmail { get; set; }
    public string? ToEmail { get; set; }
    public string? EmailMessageTemplate { get; set; }

    // Telegram
    public bool TelegramEnabled { get; set; }
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    public string? TelegramMessageTemplate { get; set; }
}
