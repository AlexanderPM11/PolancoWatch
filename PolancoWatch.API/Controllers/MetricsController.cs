using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolancoWatch.Application.Interfaces;

namespace PolancoWatch.API.Controllers;

[Authorize] // Requires JWT
[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsCollector _metricsCollector;

    public MetricsController(IMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector;
    }

    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot()
    {
        var snapshot = await _metricsCollector.CollectMetricsAsync();
        return Ok(snapshot);
    }

    [HttpPost("processes/{pid}/kill")]
    public async Task<IActionResult> KillProcess(int pid)
    {
        var result = await _metricsCollector.KillProcessAsync(pid);
        
        if (result.Success)
        {
            return Ok(new { message = result.Message });
        }

        if (result.Message.Contains("Access Denied"))
        {
            return StatusCode(403, new { message = result.Message });
        }
        
        return BadRequest(new { message = result.Message });
    }
}
