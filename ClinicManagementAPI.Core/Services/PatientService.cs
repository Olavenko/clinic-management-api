
using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;

using Microsoft.EntityFrameworkCore;

namespace ClinicManagementAPI.Core.Services;

public class PatientService(AppDbContext context) : IPatientService
{
    public async Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(PaginationRequest pagination)
    {
        // Start with IQueryable — no data fetched yet (just building the SQL)
        IQueryable<Patient> query = context.Patients;

        // Apply search filter if SearchTerm is provided
        if (!string.IsNullOrEmpty(pagination.SearchTerm))
        {
            string searchTerm = pagination.SearchTerm.Trim();

            query = query.Where(p =>
                p.FullName.Contains(searchTerm) ||
                p.Email.Contains(searchTerm) ||
                p.Phone.Contains(searchTerm));
        }

        // Get total count before pagination (for TotalCount and TotalPages)
        int totalCount = await query.CountAsync();

        // Apply ordering and pagination, then fetch from database
        List<Patient> patientEntities = await query
            .OrderBy(p => p.FullName)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        List<PatientResponse> patients = patientEntities
            .Select(MapToResponse)
            .ToList();

        // Build paged response
        var response = new PagedResponse<PatientResponse>
        {
            Items = patients,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount,
        };

        return Result<PagedResponse<PatientResponse>>.Success(response);
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(int id)
    {
        // Use FirstOrDefaultAsync instead of FindAsync to apply Global Query Filter
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
            return Result<PatientResponse>.Failure("Patient not found", 404);

        var response = MapToResponse(patient);

        return Result<PatientResponse>.Success(response);
    }

    public async Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request)
    {
        // Check email uniqueness across ALL patients (including deleted)
        bool emailExists = await context.Patients
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Email == request.Email);

        if (emailExists)
            return Result<PatientResponse>.Failure("Email already registered", 400);

        // Map DTO to entity
        var patient = new Patient
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Map entity to response
        var response = MapToResponse(patient);

        return Result<PatientResponse>.Success(response);
    }

    public async Task<Result<PatientResponse>> UpdateAsync(int id, UpdatePatientRequest request)
    {
        // Validate at least one field is provided
        bool allFieldsNull =
            request.FullName is null &&
            request.Email is null &&
            request.Phone is null &&
            request.DateOfBirth is null &&
            request.Gender is null &&
            request.Address is null;

        if (allFieldsNull)
            return Result<PatientResponse>.Failure("At least one field must be provided", 400);

        // Find patient (Global Query Filter applies)
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
            return Result<PatientResponse>.Failure("Patient not found", 404);

        // If email is being changed, check for duplicates (across all patients, exclude self)
        if (request.Email is not null && request.Email != patient.Email)
        {
            bool emailExists = await context.Patients
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Email == request.Email && p.Id != id);

            if (emailExists)
                return Result<PatientResponse>.Failure("Email already registered", 400);
        }

        // Update only provided fields (partial update)
        if (request.FullName is not null) patient.FullName = request.FullName;
        if (request.Email is not null) patient.Email = request.Email;
        if (request.Phone is not null) patient.Phone = request.Phone;
        if (request.DateOfBirth is not null) patient.DateOfBirth = request.DateOfBirth.Value;
        if (request.Gender is not null) patient.Gender = request.Gender.Value;
        if (request.Address is not null) patient.Address = request.Address;

        patient.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Map entity to response
        var response = MapToResponse(patient);

        return Result<PatientResponse>.Success(response);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        // Find patient (Global Query Filter applies)
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
            return Result<bool>.Failure("Patient not found", 404);

        // Soft delete
        patient.IsDeleted = true;
        patient.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    // Centralized mapping: Patient entity → PatientResponse DTO
    private static PatientResponse MapToResponse(Patient patient) => new()
    {
        Id = patient.Id,
        FullName = patient.FullName,
        Email = patient.Email,
        Phone = patient.Phone,
        DateOfBirth = patient.DateOfBirth,
        Gender = patient.Gender.ToString(),
        Address = patient.Address,
        CreatedAt = patient.CreatedAt,
        UpdatedAt = patient.UpdatedAt
    };
}
