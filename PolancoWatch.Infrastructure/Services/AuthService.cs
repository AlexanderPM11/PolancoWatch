using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PolancoWatch.Application.DTOs;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Infrastructure.Data;

namespace PolancoWatch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse?> AuthenticateAsync(LoginRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
        if (user == null) return null;

        bool verified = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!verified) return null;

        var token = GenerateToken(user);
        return new AuthResponse
        {
            Token = token,
            Username = user.Username
        };
    }

    public async Task<(bool Success, string Message, string? NewToken)> UpdateProfileAsync(string currentUsername, UpdateProfileRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == currentUsername);
        if (user == null) return (false, "User not found.", null);

        // Verify current password for any sensitive change
        bool verified = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!verified) return (false, "Incorrect current password.", null);

        bool usernameChanged = false;
        if (!string.IsNullOrWhiteSpace(request.NewUsername) && request.NewUsername != currentUsername)
        {
            var existingUser = await _context.Users.AnyAsync(u => u.Username == request.NewUsername);
            if (existingUser) return (false, "Username already taken.", null);
            
            user.Username = request.NewUsername;
            usernameChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        await _context.SaveChangesAsync();

        string? newToken = usernameChanged ? GenerateToken(user) : null;
        return (true, "Profile updated successfully.", newToken);
    }

    private string GenerateToken(PolancoWatch.Domain.Entities.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "super_secret_key_change_me_in_production");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
