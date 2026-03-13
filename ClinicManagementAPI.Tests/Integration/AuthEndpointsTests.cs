using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ClinicManagementAPI.Core.DTOs.Auth;

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
}
