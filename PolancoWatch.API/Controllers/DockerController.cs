using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace PolancoWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DockerController : ControllerBase
{
    private readonly ILogger<DockerController> _logger;

    public DockerController(ILogger<DockerController> logger)
    {
        _logger = logger;
    }

    [HttpPost("container/{id}/start")]
    public async Task<IActionResult> StartContainer(string id)
    {
        return await ExecuteDockerCommand(id, "start");
    }

    [HttpPost("container/{id}/stop")]
    public async Task<IActionResult> StopContainer(string id)
    {
        return await ExecuteDockerCommand(id, "stop");
    }

    [HttpPost("container/{id}/restart")]
    public async Task<IActionResult> RestartContainer(string id)
    {
        return await ExecuteDockerCommand(id, "restart");
    }

    private async Task<IActionResult> ExecuteDockerCommand(string id, string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"{command} {id}",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return BadRequest("Could not start docker process");

            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Docker {Command} failed for {Id}: {Error}", command, id, error);
                return BadRequest(new { message = error });
            }

            return Ok(new { message = $"Container {id} {command}ed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing docker {Command} for {Id}", command, id);
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
