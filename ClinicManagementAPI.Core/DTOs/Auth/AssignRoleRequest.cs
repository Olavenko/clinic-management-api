using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Auth;

// DTO for assigning a role to a user — Admin only
public class AssignRoleRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;
}
