using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.DTOs.Patients;

// DTO for updating a patient — all fields optional for partial update
// At least one field must be provided (validated in PatientService)
public class UpdatePatientRequest
{
    [MinLength(2)]
    [MaxLength(100)]
    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public Gender? Gender { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }
}
