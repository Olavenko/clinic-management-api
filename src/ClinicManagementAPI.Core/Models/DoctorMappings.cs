using ClinicManagementAPI.Core.DTOs.Doctors;

namespace ClinicManagementAPI.Core.Models;

public static class DoctorMappings
{
    public static DoctorResponse ToResponse(this Doctor doctor) => new()
    {
        Id = doctor.Id,
        FullName = doctor.FullName,
        Email = doctor.Email,
        Phone = doctor.Phone,
        Specialization = doctor.Specialization,
        YearsOfExperience = doctor.YearsOfExperience,
        Bio = doctor.Bio,
        IsAvailable = doctor.IsAvailable,
        CreatedAt = doctor.CreatedAt,
        UpdatedAt = doctor.UpdatedAt
    };
}
