using PolancoWatch.Application.DTOs;

namespace PolancoWatch.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateAsync(LoginRequest request);
}
