using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.DTOs.Appointments;

public class UpdateAppointmentStatusRequest
{
    [Required]
    public AppointmentStatus Status { get; set; }
}
