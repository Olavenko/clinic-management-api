using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken);
    Task<Result<bool>> RevokeTokenAsync(string refreshToken);
    Task<Result<bool>> AssignRoleAsync(string userId, AssignRoleRequest request);
}
