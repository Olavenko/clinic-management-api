using Microsoft.AspNetCore.Identity;

namespace ClinicManagementAPI.Core.Models;

public class ApplicationUser : IdentityUser
{
    // Patient or staff full name
    public string FullName { get; set; } = string.Empty;

    // Track when the user was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}