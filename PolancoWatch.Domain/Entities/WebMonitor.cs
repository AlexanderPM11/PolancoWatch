using System;

namespace PolancoWatch.Domain.Entities;

public class WebMonitor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int CheckIntervalSeconds { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime? LastCheckTime { get; set; }
    public bool LastStatusUp { get; set; } = true;
    public double LastLatencyMs { get; set; }
}
