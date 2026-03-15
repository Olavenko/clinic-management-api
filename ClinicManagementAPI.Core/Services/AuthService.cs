using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Services;

public class AuthService(UserManager<ApplicationUser> userManager, JwtSettings jwtSettings, AppDbContext dbContext) : IAuthService
{
    // All new registrations default to Patient role — Admin promotes via AssignRoleAsync
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
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

        var accessToken = await GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
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

        var accessToken = await GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    // Token rotation: revoke the old token and issue a new pair
    // Reuse detection: if a revoked token is presented, revoke ALL tokens for that user
    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken is null)
        {
            return Result<AuthResponse>.Failure("Invalid token", 401);
        }

        // Reuse detection: a revoked token being reused indicates possible theft
        // Revoke ALL tokens for this user as a safety measure
        if (storedToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(storedToken.UserId, cancellationToken);
            return Result<AuthResponse>.Failure("Token reuse detected — all sessions revoked", 401);
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Token expired", 401);
        }

        storedToken.IsRevoked = true;

        // Clean up expired tokens for this user to prevent table bloat
        await CleanupExpiredTokensAsync(storedToken.UserId, cancellationToken);

        var user = await userManager.FindByIdAsync(storedToken.UserId);

        var newAccessToken = await GenerateJwtToken(user!);
        var newRefreshToken = GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    // Revoke all active refresh tokens for the user (logout everywhere)
    public async Task<Result<bool>> RevokeTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken is null)
        {
            return Result<bool>.Failure("Invalid token", 400);
        }

        // Revoke all active tokens for this user, not just the submitted one
        await RevokeAllUserTokensAsync(storedToken.UserId, cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AssignRoleAsync(
        string userId, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result<bool>.Failure("User not found", 404);

        string[] validRoles = [AppRoles.Admin, AppRoles.Receptionist, AppRoles.Patient];

        if (!validRoles.Contains(request.Role))
            return Result<bool>.Failure("Invalid role", 400);

        // Design Decision: each user has exactly ONE role at a time
        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);

        await userManager.AddToRoleAsync(user, request.Role);

        return Result<bool>.Success(true);
    }

    // Revoke all active (non-revoked, non-expired) refresh tokens for a user
    private async Task RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt >= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Remove expired/revoked tokens older than the refresh token lifetime to prevent table bloat
    private async Task CleanupExpiredTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-jwtSettings.RefreshTokenExpiryDays);

        var staleTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && (rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow) && rt.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (staleTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(staleTokens);
        }
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName)
        };

        // Role claims required for RequireRole() authorization to work with JWT
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

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

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
