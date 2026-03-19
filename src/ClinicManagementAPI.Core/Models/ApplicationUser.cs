using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;

namespace ClinicManagementAPI.Core.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
