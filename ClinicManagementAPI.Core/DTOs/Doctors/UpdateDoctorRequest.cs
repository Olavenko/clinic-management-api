using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Doctors;

public class UpdateDoctorRequest
{
    [MinLength(2)]
    [MaxLength(100)]
    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Specialization { get; set; }

    [Range(0, 60)]
    public int? YearsOfExperience { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public bool? IsAvailable { get; set; }
}
