using ClinicManagementAPI.Core.DTOs.Appointments;

namespace ClinicManagementAPI.Core.Models;

public static class AppointmentMappings
{
    public static AppointmentResponse ToResponse(this Appointment appointment) => new()
    {
        Id = appointment.Id,
        PatientId = appointment.PatientId,
        PatientName = appointment.Patient?.FullName ?? "Deleted Patient",
        DoctorId = appointment.DoctorId,
        DoctorName = appointment.Doctor?.FullName ?? "Deleted Doctor",
        DoctorSpecialization = appointment.Doctor?.Specialization ?? "Unknown",
        AppointmentDate = appointment.AppointmentDate,
        AppointmentTime = appointment.AppointmentTime,
        DurationMinutes = appointment.DurationMinutes,
        Status = appointment.Status.ToString(),
        Notes = appointment.Notes,
        CreatedAt = appointment.CreatedAt,
        UpdatedAt = appointment.UpdatedAt
    };
}
