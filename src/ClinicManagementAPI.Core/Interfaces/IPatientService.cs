using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

// Contract for Patient CRUD operations — all methods return Result<T>
public interface IPatientService
{
    Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<Result<PatientResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<Result<PatientResponse>> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
