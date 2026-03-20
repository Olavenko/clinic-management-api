using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Models;
using ClinicManagementAPI.Core.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementAPI.Tests.Unit;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup DI container with Identity + InMemory DB
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

        var serviceProvider = services.BuildServiceProvider();

        _userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        // Seed roles (mirrors DatabaseSeeder.SeedRolesAsync used in production)
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Doctor, AppRoles.Receptionist, AppRoles.Patient })
        {
            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }

        // JWT settings for testing
        var jwtSettings = new JwtSettings
        {
            Key = "ThisIsATestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7
        };

        _authService = new AuthService(_userManager, jwtSettings, _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Register ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "test@clinic.com",
            Password = "Test1234!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailureResult()
    {
        // Arrange — register first user
        var request = new RegisterRequest
        {
            FullName = "First User",
            Email = "duplicate@clinic.com",
            Password = "Test1234!"
        };
        await _authService.RegisterAsync(request);

        // Arrange — same email, different user
        var duplicateRequest = new RegisterRequest
        {
            FullName = "Second User",
            Email = "duplicate@clinic.com",
            Password = "Test5678!"
        };

        // Act
        var result = await _authService.RegisterAsync(duplicateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    // ── Login ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange — register a user first
        var registerRequest = new RegisterRequest
        {
            FullName = "Login User",
            Email = "login@clinic.com",
            Password = "Test1234!"
        };
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@clinic.com",
            Password = "Test1234!"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailureResult()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FullName = "Login User",
            Email = "wrong@clinic.com",
            Password = "Test1234!"
        };
        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "wrong@clinic.com",
            Password = "WrongPassword!"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsFailureResult()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nobody@clinic.com",
            Password = "Test1234!"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Invalid credentials", result.Error);
    }

    // ── RefreshToken ───────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsSuccessResult()
    {
        // Arrange — register to get a refresh token
        var registerRequest = new RegisterRequest
        {
            FullName = "Refresh User",
            Email = "refresh@clinic.com",
            Password = "Test1234!"
        };
        var registerResult = await _authService.RegisterAsync(registerRequest);
        var refreshToken = registerResult.Value!.RefreshToken;

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(refreshToken, result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsFailureResult()
    {
        // Arrange — create an expired token directly in DB
        var user = new ApplicationUser
        {
            FullName = "Expired User",
            Email = "expired@clinic.com",
            UserName = "expired@clinic.com"
        };
        await _userManager.CreateAsync(user, "Test1234!");

        var expiredToken = new RefreshToken
        {
            Token = "expired-token-string",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.RefreshTokenAsync("expired-token-string");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
    }

    // ── RevokeToken ────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ReturnsSuccessResult()
    {
        // Arrange — register to get a refresh token
        var registerRequest = new RegisterRequest
        {
            FullName = "Revoke User",
            Email = "revoke@clinic.com",
            Password = "Test1234!"
        };
        var registerResult = await _authService.RegisterAsync(registerRequest);
        var refreshToken = registerResult.Value!.RefreshToken;

        // Act
        var result = await _authService.RevokeTokenAsync(refreshToken);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify token is actually revoked in DB
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        Assert.True(storedToken!.IsRevoked);
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ReturnsFailureResult()
    {
        // Arrange — weak password (e.g., no uppercase, no numbers, etc. depending on Identity setup)
        var request = new RegisterRequest
        {
            FullName = "Weak Password User",
            Email = "weak@clinic.com",
            Password = "weak" // Identity requires By Default: 6 chars, 1 uppercase, 1 lowercase, 1 digit, 1 non-alphanumeric
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNonExistentToken_ReturnsFailureResult()
    {
        // Act
        var result = await _authService.RefreshTokenAsync("this-token-does-not-exist");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Invalid token", result.Error);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ReturnsFailureResult()
    {
        // Arrange — register to get a refresh token
        var registerRequest = new RegisterRequest
        {
            FullName = "Revoked Refresh User",
            Email = "revoked_refresh@clinic.com",
            Password = "Test1234!"
        };
        var registerResult = await _authService.RegisterAsync(registerRequest);
        var refreshToken = registerResult.Value!.RefreshToken;

        // Revoke the token
        await _authService.RevokeTokenAsync(refreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Token reuse detected — all sessions revoked", result.Error);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ReturnsFailureResult()
    {
        // Act
        var result = await _authService.RevokeTokenAsync("this-token-does-not-exist");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid token", result.Error);
    }

    // ── AssignRole ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleAsync_WithValidUserId_ReturnsSuccessResult()
    {
        // Arrange — register a user (defaults to Patient role)
        var registerRequest = new RegisterRequest
        {
            FullName = "Role User",
            Email = "role@clinic.com",
            Password = "Test1234!"
        };
        await _authService.RegisterAsync(registerRequest);

        var user = await _userManager.FindByEmailAsync("role@clinic.com");
        var assignRequest = new AssignRoleRequest { Role = AppRoles.Receptionist };

        // Act
        var result = await _authService.AssignRoleAsync(user!.Id, assignRequest);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify role was changed
        var roles = await _userManager.GetRolesAsync(user);
        Assert.Single(roles);
        Assert.Contains(AppRoles.Receptionist, roles);
    }

    [Fact]
    public async Task AssignRoleAsync_WithInvalidUserId_ReturnsFailureResult()
    {
        // Arrange
        var assignRequest = new AssignRoleRequest { Role = AppRoles.Admin };

        // Act
        var result = await _authService.AssignRoleAsync("non-existent-user-id", assignRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("User not found", result.Error);
    }

    // ── T6: Token Rotation Reuse Detection ─────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ReuseAfterRotation_RevokesAllTokens()
    {
        // Arrange — register to get first refresh token
        var registerRequest = new RegisterRequest
        {
            FullName = "Rotation User",
            Email = "rotation@clinic.com",
            Password = "Test1234!"
        };
        var registerResult = await _authService.RegisterAsync(registerRequest);
        var firstToken = registerResult.Value!.RefreshToken;

        // Refresh once — first token is now revoked, second token issued
        var refreshResult = await _authService.RefreshTokenAsync(firstToken);
        Assert.True(refreshResult.IsSuccess);
        var secondToken = refreshResult.Value!.RefreshToken;

        // Act — reuse the first (now-revoked) token
        var reuseResult = await _authService.RefreshTokenAsync(firstToken);

        // Assert — reuse detected, all tokens revoked
        Assert.False(reuseResult.IsSuccess);
        Assert.Equal(401, reuseResult.StatusCode);
        Assert.Equal("Token reuse detected — all sessions revoked", reuseResult.Error);

        // Second token should also be revoked now
        var secondTokenResult = await _authService.RefreshTokenAsync(secondToken);
        Assert.False(secondTokenResult.IsSuccess);
    }

    // ── T7: Token Cleanup Persistence (B1 fix) ─────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_CleansUpExpiredTokens()
    {
        // Arrange — register a user
        var registerRequest = new RegisterRequest
        {
            FullName = "Cleanup User",
            Email = "cleanup@clinic.com",
            Password = "Test1234!"
        };
        var registerResult = await _authService.RegisterAsync(registerRequest);
        var userId = (await _userManager.FindByEmailAsync("cleanup@clinic.com"))!.Id;

        // Seed an old expired token directly in DB
        var expiredToken = new RefreshToken
        {
            Token = "old-expired-token",
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            IsRevoked = true
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act — refresh triggers cleanup
        var refreshResult = await _authService.RefreshTokenAsync(registerResult.Value!.RefreshToken);
        Assert.True(refreshResult.IsSuccess);

        // Assert — expired token should be cleaned up (removed from DB)
        var expiredStillExists = _dbContext.RefreshTokens.Any(rt => rt.Token == "old-expired-token");
        Assert.False(expiredStillExists);
    }

    // ── AssignRole ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleAsync_WithInvalidRole_ReturnsFailureResult()
    {
        // Arrange — register a user
        var registerRequest = new RegisterRequest
        {
            FullName = "Invalid Role User",
            Email = "invalidrole@clinic.com",
            Password = "Test1234!"
        };
        await _authService.RegisterAsync(registerRequest);

        var user = await _userManager.FindByEmailAsync("invalidrole@clinic.com");
        var assignRequest = new AssignRoleRequest { Role = "SuperAdmin" }; // not a valid role

        // Act
        var result = await _authService.AssignRoleAsync(user!.Id, assignRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Invalid role", result.Error);
    }
}
