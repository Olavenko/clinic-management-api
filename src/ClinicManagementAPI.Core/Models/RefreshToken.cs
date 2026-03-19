using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.Models;

public class RefreshToken
{
    public int Id { get; set; }

    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
