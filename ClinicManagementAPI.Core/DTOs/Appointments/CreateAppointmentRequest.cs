using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    public DateOnly AppointmentDate { get; set; }

    [Required]
    public TimeOnly AppointmentTime { get; set; }

    [Range(15, 120)]
    public int DurationMinutes { get; set; } = 30;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
