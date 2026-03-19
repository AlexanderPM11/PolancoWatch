using System;

namespace PolancoWatch.Domain.Entities;

public class WebCheck
{
    public int Id { get; set; }
    public int WebMonitorId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsUp { get; set; }
    public double LatencyMs { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}
