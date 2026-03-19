using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.DTOs.Appointments;

public class AppointmentFilterRequest : PaginationRequest
{
    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public AppointmentStatus? Status { get; set; }
}
