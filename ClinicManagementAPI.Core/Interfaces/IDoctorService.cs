using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

// Contract for Doctor CRUD operations — all methods return Result<T>
public interface IDoctorService
{
    Task<Result<PagedResponse<DoctorResponse>>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<PagedResponse<DoctorResponse>>> GetAvailableAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<DoctorResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<DoctorResponse>> CreateAsync(CreateDoctorRequest request, CancellationToken cancellationToken = default);
    Task<Result<DoctorResponse>> UpdateAsync(int id, UpdateDoctorRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
