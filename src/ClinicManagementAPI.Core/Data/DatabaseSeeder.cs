using ClinicManagementAPI.Core.Models;

using Microsoft.AspNetCore.Identity;

namespace ClinicManagementAPI.Core.Data;

public static class DatabaseSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { AppRoles.Admin, AppRoles.Doctor, AppRoles.Receptionist, AppRoles.Patient };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
