using ClinicManagementAPI.Api.Filters;
using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        // POST /api/auth/register
        group.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        // POST /api/auth/login
        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        // POST /api/auth/refresh
        group.MapPost("/refresh", Refresh)
            .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>();

        // POST /api/auth/logout
        group.MapPost("/logout", Logout)
            .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>()
            .RequireAuthorization();
    }

    private static async Task<IResult> Register(
        RegisterRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created("", result.Value)
            : Results.Problem(
                title: "Registration failed",
                detail: result.Error,
                statusCode: result.StatusCode);
    }

    private static async Task<IResult> Login(
        LoginRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Login failed",
                detail: result.Error,
                statusCode: result.StatusCode);
    }

    private static async Task<IResult> Refresh(
        RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Refresh failed",
                detail: result.Error,
                statusCode: result.StatusCode);
    }

    private static async Task<IResult> Logout(
        RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(
                title: "Logout failed",
                detail: result.Error,
                statusCode: result.StatusCode);
    }
}
