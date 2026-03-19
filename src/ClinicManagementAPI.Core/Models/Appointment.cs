using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.Models;

// Appointment entity — uses Status lifecycle instead of soft delete
// Only Cancelled appointments can be hard deleted (preserves audit trail)
public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public int DurationMinutes { get; set; } = 30;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
