using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

// Contract for authentication operations — all methods return Result<T>
public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> AssignRoleAsync(string userId, AssignRoleRequest request, CancellationToken cancellationToken = default);
}
