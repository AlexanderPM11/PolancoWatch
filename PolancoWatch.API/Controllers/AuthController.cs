using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PolancoWatch.Application.DTOs;
using PolancoWatch.Application.Interfaces;

namespace PolancoWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.AuthenticateAsync(request);
        if (response == null) return Unauthorized(new { message = "Invalid username or password" });

        return Ok(response);
    }

    [Authorize]
    [HttpPost("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var result = await _authService.UpdateProfileAsync(username, request);
        if (!result.Success) return BadRequest(new { message = result.Message });

        return Ok(new { 
            message = result.Message,
            token = result.NewToken,
            username = request.NewUsername ?? username
        });
    }
}
