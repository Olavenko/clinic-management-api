using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Appointments;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

// Contract for Appointment operations — all methods return Result<T>
public interface IAppointmentService
{
    Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(AppointmentFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<AppointmentResponse>>> GetByPatientAsync(int patientId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<AppointmentResponse>>> GetByDoctorAsync(int doctorId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<AppointmentResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<AppointmentResponse>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<AppointmentResponse>> UpdateAsync(int id, UpdateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<AppointmentResponse>> UpdateStatusAsync(int id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
