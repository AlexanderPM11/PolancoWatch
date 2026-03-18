namespace PolancoWatch.Domain.Entities;

public enum MetricType
{
    Cpu,
    Memory,
    Disk
}

public class AlertRule
{
    public int Id { get; set; }
    public MetricType MetricType { get; set; }
    public double Threshold { get; set; } // e.g., 80 for 80%
    public int CooldownSeconds { get; set; } = 300; // Default 5 minutes
    public bool IsActive { get; set; } = true;
}

public class AlertHistory
{
    public int Id { get; set; }
    public int AlertRuleId { get; set; }
    public AlertRule AlertRule { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}

public class NotificationSettings
{
    public int Id { get; set; }
    
    // Telegram Settings
    public bool TelegramEnabled { get; set; }
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }

    // Email Settings
    public bool EmailEnabled { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpUser { get; set; }
    public string? SmtpPass { get; set; }
    public string? FromEmail { get; set; }
    public string? ToEmail { get; set; }
}
