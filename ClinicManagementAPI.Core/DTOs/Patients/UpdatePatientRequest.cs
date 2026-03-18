using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.DTOs.Patients;

// At least one field must be provided (validated in PatientService)
public class UpdatePatientRequest
{
    [MinLength(2)]
    [MaxLength(100)]
    public string? FullName { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    public Gender? Gender { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}
