using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.DTOs.Patients;

public class CreatePatientRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}
