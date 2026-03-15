using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

// Contract for Doctor CRUD operations — all methods return Result<T>
public interface IDoctorService
{
    Task<Result<PagedResponse<DoctorResponse>>> GetAllAsync(PaginationRequest pagination);
    Task<Result<PagedResponse<DoctorResponse>>> GetAvailableAsync(PaginationRequest pagination);
    Task<Result<DoctorResponse>> GetByIdAsync(int id);
    Task<Result<DoctorResponse>> CreateAsync(CreateDoctorRequest request);
    Task<Result<DoctorResponse>> UpdateAsync(int id, UpdateDoctorRequest request);
    Task<Result<bool>> DeleteAsync(int id);
}
