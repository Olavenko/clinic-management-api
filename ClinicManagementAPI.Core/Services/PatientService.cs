using Microsoft.EntityFrameworkCore;

using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Services;

public class PatientService(AppDbContext context) : IPatientService
{
    public async Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(
        PaginationRequest pagination, CancellationToken cancellationToken = default)
    {

        IQueryable<Patient> query = context.Patients;

        if (!string.IsNullOrEmpty(pagination.SearchTerm))
        {
            string searchTerm = pagination.SearchTerm.Trim().ToLower();

            query = query.Where(p =>
                p.FullName.ToLower().Contains(searchTerm) ||
                p.Email.ToLower().Contains(searchTerm) ||
                p.Phone.ToLower().Contains(searchTerm));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<Patient> patientEntities = await query
            .OrderBy(p => p.FullName)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        List<PatientResponse> patients = patientEntities.Select(p => p.ToResponse()).ToList();

        var response = new PagedResponse<PatientResponse>
        {
            Items = patients,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount,
        };

        return Result<PagedResponse<PatientResponse>>.Success(response);
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        // FirstOrDefaultAsync (not FindAsync) to apply the global soft-delete query filter
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return patient is null
            ? Result<PatientResponse>.Failure("Patient not found", 404)
            : Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<Result<PatientResponse>> CreateAsync(
        CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: prevent email reuse even if prior record was soft-deleted
        bool emailExists = await context.Patients
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Email == request.Email, cancellationToken);

        if (emailExists)
            return Result<PatientResponse>.Failure("Email already registered", 400);

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

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<PatientResponse>.Failure("Email already registered", 400);
        }

        return Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<Result<PatientResponse>> UpdateAsync(
        int id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        bool allFieldsNull =
            request.FullName is null &&
            request.Email is null &&
            request.Phone is null &&
            request.DateOfBirth is null &&
            request.Gender is null &&
            request.Address is null;

        if (allFieldsNull)
            return Result<PatientResponse>.Failure("At least one field must be provided", 400);

        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (patient is null)
            return Result<PatientResponse>.Failure("Patient not found", 404);

        // IgnoreQueryFilters: prevent email reuse even if prior record was soft-deleted
        if (request.Email is not null && request.Email != patient.Email)
        {
            bool emailExists = await context.Patients
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Email == request.Email && p.Id != id, cancellationToken);

            if (emailExists)
                return Result<PatientResponse>.Failure("Email already registered", 400);
        }

        if (request.FullName is not null) patient.FullName = request.FullName;
        if (request.Email is not null) patient.Email = request.Email;
        if (request.Phone is not null) patient.Phone = request.Phone;
        if (request.DateOfBirth is not null) patient.DateOfBirth = request.DateOfBirth.Value;
        if (request.Gender is not null) patient.Gender = request.Gender.Value;
        if (request.Address is not null) patient.Address = request.Address;

        patient.UpdatedAt = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<PatientResponse>.Failure("Email already registered", 400);
        }

        return Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<Result<bool>> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (patient is null)
            return Result<bool>.Failure("Patient not found", 404);

        patient.IsDeleted = true;
        patient.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
