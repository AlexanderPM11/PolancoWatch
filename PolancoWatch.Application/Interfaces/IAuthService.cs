using PolancoWatch.Application.DTOs;

namespace PolancoWatch.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateAsync(LoginRequest request);
    Task<(bool Success, string Message, string? NewToken)> UpdateProfileAsync(string currentUsername, UpdateProfileRequest request);
}
