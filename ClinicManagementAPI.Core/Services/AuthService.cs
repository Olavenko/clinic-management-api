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
        // Step 1: Check if email is already taken
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result<AuthResponse>.Failure("Email already registered", 400);
        }

        // Step 2: Create new user object
        var user = new ApplicationUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email
        };

        // Step 3: Save user to database with hashed password
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Failure(errors, 400);
        }

        // Step 4: Assign default role
        await userManager.AddToRoleAsync(user, AppRoles.Patient);

        // Step 5: Generate tokens
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        // Step 6: Save refresh token in Database
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        // Step 7: Return success response
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
        // Step 1: Find user by email
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result<AuthResponse>.Failure("Invalid credentials", 401);
        }

        // Step 2: Check if password is valid
        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure("Invalid credentials", 401);
        }

        // Step 3: Generate tokens
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        // Step 4: Save refresh token in Database
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        // Step 5: Return success response
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
        // Step 1: Find the token in Database
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null)
        {
            return Result<AuthResponse>.Failure("Invalid token", 401);
        }

        // Step 2: Validate it is not revoked and not expired
        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Token expired or revoked", 401);
        }

        // Step 3: Revoke the old token
        storedToken.IsRevoked = true;

        // Step 4: Get the user who owns this token
        var user = await userManager.FindByIdAsync(storedToken.UserId);

        // Step 5: Generate new tokens
        var newAccessToken = GenerateJwtToken(user!);
        var newRefreshToken = GenerateRefreshToken();

        // Step 6: Save new refresh token in Database
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        // Step 7: Return new tokens
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
        // Step 1: Find the token in Database
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null)
        {
            return Result<bool>.Failure("Invalid token", 400);
        }

        // Step 2: Revoke the old token
        storedToken.IsRevoked = true;

        // Step 3: Save new refresh token in Database
        await dbContext.SaveChangesAsync();

        // Step 4: Return success
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
}
