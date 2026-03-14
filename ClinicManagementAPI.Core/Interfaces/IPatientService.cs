using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Interfaces;

// Contract for Patient CRUD operations — all methods return Result<T>
public interface IPatientService
{
    Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(PaginationRequest pagination);
    Task<Result<PatientResponse>> GetByIdAsync(int id);
    Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request);
    Task<Result<PatientResponse>> UpdateAsync(int id, UpdatePatientRequest request);
    Task<Result<bool>> DeleteAsync(int id);
}
