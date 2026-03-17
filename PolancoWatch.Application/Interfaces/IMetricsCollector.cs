using PolancoWatch.Domain.Models;

namespace PolancoWatch.Application.Interfaces;

public interface IMetricsCollector
{
    Task<ServerMetricsSnapshot> CollectMetricsAsync();
}
