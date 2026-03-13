using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementAPI.Core.Services;

public class AuthService(UserManager<ApplicationUser> userManager, JwtSettings jwtSettings) : IAuthService
{

    // Register a new user — always assigned Patient role by default
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Check if email is already taken
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result<AuthResponse>.Failure("Email already registered", 400);
        }

        // Create new user object
        var user = new ApplicationUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email
        };

        // Save user to database with hashed password
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Failure(errors, 400);
        }

        // Assign default role
        await userManager.AddToRoleAsync(user, AppRoles.Patient);

        // Generate tokens
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

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
        // Find user by email
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result<AuthResponse>.Failure("Invalid credentials", 401);
        }

        // Check if password is valid
        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure("Invalid credentials", 401);
        }

        // Generate tokens
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        // Return success response
        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes)
        });
    }

    // Will be implemented in Section 7
    public Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
        => throw new NotImplementedException();

    // Will be implemented in Section 7
    public Task<Result<bool>> RevokeTokenAsync(string refreshToken)
        => throw new NotImplementedException();

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
