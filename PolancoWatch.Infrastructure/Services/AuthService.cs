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
    private readonly IEmailService _emailService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
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

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return (true, "If an account with that email exists, a reset link will be sent."); // Security: don't reveal if user exists

        var token = Guid.NewGuid().ToString();
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var settings = await _context.NotificationSettings.FirstOrDefaultAsync();
        var resetLink = $"{_configuration["AppUrl"]}/reset-password?token={token}";
        var body = $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #6366f1; border-radius: 8px;'>
                <h2 style='color: #6366f1;'>PolancoWatch Password Reset</h2>
                <p>You requested a password reset. Click the button below to set a new password:</p>
                <a href='{resetLink}' style='display: inline-block; padding: 12px 24px; background-color: #6366f1; color: white; text-decoration: none; border-radius: 6px; font-weight: bold;'>Reset Password</a>
                <p>Or copy this link: {resetLink}</p>
                <hr/>
                <p style='font-size: 12px; color: #666;'>If you didn't request this, you can ignore this email. This link expires in 1 hour.</p>
            </div>";

        await _emailService.SendEmailAsync(user.Email, "PolancoWatch: Password Reset Request", body, settings);

        return (true, "If an account with that email exists, a reset link will be sent.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.ResetToken == request.Token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null) return (false, "Invalid or expired reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _context.SaveChangesAsync();

        return (true, "Password reset successfully.");
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
