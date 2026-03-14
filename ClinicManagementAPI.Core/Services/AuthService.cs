using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;
using ClinicManagementAPI.Core.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementAPI.Core.Services;

public class AuthService(UserManager<ApplicationUser> userManager, JwtSettings jwtSettings, AppDbContext dbContext) : IAuthService
{
    // Register a new user — always assigned Patient role by default
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result<AuthResponse>.Failure("Email already registered", 400);
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Failure(errors, 400);
        }

        await userManager.AddToRoleAsync(user, AppRoles.Patient);

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    // Login an existing user
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result<AuthResponse>.Failure("Invalid credentials", 401);
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure("Invalid credentials", 401);
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    // Refresh an expired JWT using a valid refresh token
    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null)
        {
            return Result<AuthResponse>.Failure("Invalid token", 401);
        }

        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Token expired or revoked", 401);
        }

        storedToken.IsRevoked = true;

        var user = await userManager.FindByIdAsync(storedToken.UserId);

        var newAccessToken = GenerateJwtToken(user!);
        var newRefreshToken = GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    // Revoke a refresh token (logout)
    public async Task<Result<bool>> RevokeTokenAsync(string refreshToken)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null)
        {
            return Result<bool>.Failure("Invalid token", 400);
        }

        storedToken.IsRevoked = true;
        await dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    // Generate JWT token with user claims
    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Generate cryptographically secure random refresh token
    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // Assign a role to a user — Admin only
    public async Task<Result<bool>> AssignRoleAsync(string userId, AssignRoleRequest request)
    {
        // Find user by ID
        ApplicationUser? user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result<bool>.Failure("User not found", 404);

        // Validate role exists in AppRoles
        string[] validRoles = [AppRoles.Admin, AppRoles.Receptionist, AppRoles.Patient];

        if (!validRoles.Contains(request.Role))
            return Result<bool>.Failure("Invalid role", 400);

        // Remove all current roles
        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);

        // Assign new role
        await userManager.AddToRoleAsync(user, request.Role);

        return Result<bool>.Success(true);
    }
}
