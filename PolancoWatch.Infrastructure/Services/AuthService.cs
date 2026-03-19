using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PolancoWatch.Application.DTOs;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Infrastructure.Data;
using PolancoWatch.Domain.Entities;

namespace PolancoWatch.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ITelegramService _telegramService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService, ITelegramService telegramService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _telegramService = telegramService;
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
        var user = await _context.Users.SingleOrDefaultAsync(u => 
            (!string.IsNullOrEmpty(request.Email) && u.Email == request.Email) ||
            (!string.IsNullOrEmpty(request.Username) && u.Username == request.Username));

        var settings = await _context.NotificationSettings.FirstOrDefaultAsync();
        bool isTelegramConfigured = settings != null && 
                                  !string.IsNullOrWhiteSpace(settings.TelegramBotToken) && 
                                  !string.IsNullOrWhiteSpace(settings.TelegramChatId);

        if (!isTelegramConfigured)
        {
            return (false, "ERROR_TELEGRAM_NOT_CONFIGURED");
        }

        if (user == null) return (true, "Recovery protocol initiated."); 

        var token = Guid.NewGuid().ToString();
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var appUrl = Environment.GetEnvironmentVariable("APP_URL") ?? 
                     _configuration["APP_URL"] ?? 
                     "http://localhost:5173";
        var resetLink = $"{appUrl}/reset-password?token={token}";
        
        var message = $@"*PolancoWatch Recovery Protocol*

A password reset has been requested for the user: *{user.Username}*

Copy and paste the link below in your browser to set a new security key:
{resetLink}

_If you didn't request this, you can ignore this message._";

        await _telegramService.SendMessageAsync(message, settings);

        return (true, "Recovery protocol initiated. Check your Telegram bot.");
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

    private string GenerateToken(User user)
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
