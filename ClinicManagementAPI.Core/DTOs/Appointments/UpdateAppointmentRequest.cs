using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Appointments;

public class UpdateAppointmentRequest
{
    public DateOnly? AppointmentDate { get; set; }

    public TimeOnly? AppointmentTime { get; set; }

    [Range(15, 120)]
    public int? DurationMinutes { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
