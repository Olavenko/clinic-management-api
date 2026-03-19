using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
