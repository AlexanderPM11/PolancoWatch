using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;

namespace PolancoWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DockerController : ControllerBase
{
    private readonly ILogger<DockerController> _logger;
    private readonly IDockerClient _dockerClient;

    public DockerController(ILogger<DockerController> logger, IDockerClient dockerClient)
    {
        _logger = logger;
        _dockerClient = dockerClient;
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
            switch (command.ToLower())
            {
                case "start":
                    await _dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters());
                    break;
                case "stop":
                    await _dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters());
                    break;
                case "restart":
                    await _dockerClient.Containers.RestartContainerAsync(id, new ContainerRestartParameters());
                    break;
                default:
                    return BadRequest($"Unknown command: {command}");
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
