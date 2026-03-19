using ClinicManagementAPI.Core.DTOs.Patients;

namespace ClinicManagementAPI.Core.Models;

public static class PatientMappings
{
    public static PatientResponse ToResponse(this Patient patient) => new()
    {
        Id = patient.Id,
        FullName = patient.FullName,
        Email = patient.Email,
        Phone = patient.Phone,
        DateOfBirth = patient.DateOfBirth,
        Gender = patient.Gender.ToString(),
        Address = patient.Address,
        CreatedAt = patient.CreatedAt,
        UpdatedAt = patient.UpdatedAt
    };
}
