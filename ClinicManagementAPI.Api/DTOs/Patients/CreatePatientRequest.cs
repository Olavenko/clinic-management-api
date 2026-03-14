using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Api.DTOs.Patients;

public class CreatePatientRequest
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
    public Gender Gender { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }
}
