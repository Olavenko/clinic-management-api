using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
