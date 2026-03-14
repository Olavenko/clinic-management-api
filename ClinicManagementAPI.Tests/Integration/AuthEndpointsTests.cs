using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementAPI.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Helper Methods ──────────────────────────────────────────────

    /// <summary>
    /// Registers a user and assigns the specified role via UserManager.
    /// Returns the access token with correct role claims.
    /// </summary>
    private async Task<string> GetTokenForRoleAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        var registerRequest = new RegisterRequest
        {
            FullName = $"{role} User",
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        if (role != "Patient")
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.RemoveFromRoleAsync(user!, AppRoles.Patient);
            await userManager.AddToRoleAsync(user!, role);
        }

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authData!.AccessToken;
    }

    private static string GetUserIdFromToken(string accessToken)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        return token.Claims.First(c => c.Type == "sub").Value;
    }

    // ── Auth Endpoint Tests (existing) ──────────────────────────────

    // Test 1: Register with valid data
    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        // Arrange
        var uniqueEmail = $"valid_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest
        {
            FullName = "Valid User",
            Email = uniqueEmail,
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.AccessToken);
        Assert.NotEmpty(authResponse.RefreshToken);
    }

    // Test 2: Register with missing fields
    [Fact]
    public async Task Register_WithMissingFields_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            // Missing FullName, Email, and Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test 3: Register with invalid email format
    [Fact]
    public async Task Register_WithInvalidEmailFormat_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "invalid-email-format",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test 4: Register with short password
    [Fact]
    public async Task Register_WithShortPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "short" // < 8 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test 5: Register with duplicate email
    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = email,
            Password = "Password123!"
        };

        // Register first time
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - Register second time with duplicate email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test 6: Login with valid credentials
    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var email = $"login_{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        var registerRequest = new RegisterRequest
        {
            FullName = "Login User",
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.AccessToken);
    }

    // Test 7: Login with wrong password
    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        // Arrange
        var email = $"loginwrong_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            FullName = "Login Wrong User",
            Email = email,
            Password = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 8: Login with non-existent email
    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = $"nonexistent_{Guid.NewGuid()}@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 9: Refresh with valid token
    [Fact]
    public async Task Refresh_WithValidToken_Returns200Ok()
    {
        // Arrange
        var email = $"refresh_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            FullName = "Refresh User",
            Email = email,
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = authData!.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshData = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshData);
        Assert.NotEmpty(refreshData.AccessToken);
        Assert.NotEmpty(refreshData.RefreshToken);
        Assert.NotEqual(authData.RefreshToken, refreshData.RefreshToken);
    }

    // Test 10: Refresh with expired or invalid token
    [Fact]
    public async Task Refresh_WithExpiredOrInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 11: Logout on success
    [Fact]
    public async Task Logout_OnSuccess_Returns204NoContent()
    {
        // Arrange
        var email = $"logout_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            FullName = "Logout User",
            Email = email,
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = authData!.RefreshToken
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(logoutRequest)
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // Test 12: Logout without JWT
    [Fact]
    public async Task Logout_WithoutJwt_Returns401Unauthorized()
    {
        // Arrange
        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = "some-token"
        };

        // Act - no bearer token sent
        var response = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Role Assignment Tests ───────────────────────────────────────

    // Test 13: Assign role with Admin token + valid role
    [Fact]
    public async Task AssignRole_WithAdminTokenAndValidRole_Returns200()
    {
        // Arrange — get admin token and a target user
        var adminToken = await GetTokenForRoleAsync("Admin");

        // Register a target user (default Patient role)
        var targetEmail = $"target_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            FullName = "Target User",
            Email = targetEmail,
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var targetAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var targetUserId = GetUserIdFromToken(targetAuth!.AccessToken);

        var assignRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{targetUserId}/role")
        {
            Content = JsonContent.Create(new AssignRoleRequest { Role = "Receptionist" })
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(assignRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Test 14: Assign role with invalid role name
    [Fact]
    public async Task AssignRole_WithInvalidRole_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");

        var targetEmail = $"target_{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Target User",
            Email = targetEmail,
            Password = "Password123!"
        });

        // Get userId from login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = targetEmail,
            Password = "Password123!"
        });
        var targetAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var targetUserId = GetUserIdFromToken(targetAuth!.AccessToken);

        var assignRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{targetUserId}/role")
        {
            Content = JsonContent.Create(new AssignRoleRequest { Role = "SuperAdmin" }) // invalid role
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(assignRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test 15: Assign role with Receptionist token → 403
    [Fact]
    public async Task AssignRole_WithReceptionistToken_Returns403()
    {
        // Arrange
        var receptionistToken = await GetTokenForRoleAsync("Receptionist");

        var assignRequest = new HttpRequestMessage(HttpMethod.Put, "/api/users/some-user-id/role")
        {
            Content = JsonContent.Create(new AssignRoleRequest { Role = "Admin" })
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", receptionistToken);

        // Act
        var response = await _client.SendAsync(assignRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test 16: Assign role with invalid userId → 404
    [Fact]
    public async Task AssignRole_WithInvalidUserId_Returns404()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");

        var assignRequest = new HttpRequestMessage(HttpMethod.Put, "/api/users/non-existent-id/role")
        {
            Content = JsonContent.Create(new AssignRoleRequest { Role = "Receptionist" })
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(assignRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
