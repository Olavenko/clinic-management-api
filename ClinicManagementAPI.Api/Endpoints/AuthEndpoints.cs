using ClinicManagementAPI.Api.Filters;
using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Api.Endpoints;

public static class AuthEndpoints
{
    // Extension method on WebApplication — keeps Program.cs clean
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Group all auth endpoints under /api/auth
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        // POST /api/auth/register
        group.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);

            return result.IsSuccess
                ? Results.Created("", result.Value)
                : Results.Problem(
                    title: "Registration failed",
                    detail: result.Error,
                    statusCode: result.StatusCode);
        })
        .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        // POST /api/auth/login
        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: "Login failed",
                    detail: result.Error,
                    statusCode: result.StatusCode);
        })
        .AddEndpointFilter<ValidationFilter<LoginRequest>>();
    }
}
