using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Doctors;

public class CreateDoctorRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required]
    [Range(0, 60)]
    public int YearsOfExperience { get; set; }

    public string? Bio { get; set; }
}
