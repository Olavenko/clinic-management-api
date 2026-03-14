using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // PUT /api/users/{id}/role — Admin ONLY
        group.MapPut("/{id}/role", async (
            string id,
            AssignRoleRequest request,
            IAuthService authService) =>
        {
            var result = await authService.AssignRoleAsync(id, request);

            return result.IsSuccess
                ? Results.Ok(new { message = "Role assigned successfully" })
                : Results.Problem(result.Error!, statusCode: result.StatusCode);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
