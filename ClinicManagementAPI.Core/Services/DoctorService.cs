using Microsoft.EntityFrameworkCore;

using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Services;

public class DoctorService(AppDbContext context) : IDoctorService
{
    public async Task<Result<PagedResponse<DoctorResponse>>> GetAllAsync(
        PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        IQueryable<Doctor> query = context.Doctors;

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            string searchTerm = pagination.SearchTerm.Trim().ToLower();

            query = query.Where(d =>
                d.FullName.ToLower().Contains(searchTerm) ||
                d.Specialization.ToLower().Contains(searchTerm));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<DoctorResponse> doctors = (await query
            .OrderBy(d => d.FullName)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken))
            .Select(d => d.ToResponse())
            .ToList();

        var response = new PagedResponse<DoctorResponse>
        {
            Items = doctors,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Result<PagedResponse<DoctorResponse>>.Success(response);
    }

    public async Task<Result<PagedResponse<DoctorResponse>>> GetAvailableAsync(
        PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        IQueryable<Doctor> query = context.Doctors.Where(d => d.IsAvailable);

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            string searchTerm = pagination.SearchTerm.Trim().ToLower();

            query = query.Where(d =>
                d.FullName.ToLower().Contains(searchTerm) ||
                d.Specialization.ToLower().Contains(searchTerm));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<DoctorResponse> doctors = (await query
            .OrderBy(d => d.FullName)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken))
            .Select(d => d.ToResponse())
            .ToList();

        var response = new PagedResponse<DoctorResponse>
        {
            Items = doctors,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Result<PagedResponse<DoctorResponse>>.Success(response);
    }

    public async Task<Result<DoctorResponse>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        // FirstOrDefaultAsync (not FindAsync) to apply the global soft-delete query filter
        Doctor? doctor = await context.Doctors
           .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return doctor is null
            ? Result<DoctorResponse>.Failure("Doctor not found", 404)
            : Result<DoctorResponse>.Success(doctor.ToResponse());
    }

    public async Task<Result<DoctorResponse>> CreateAsync(
        CreateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: prevent email reuse even if prior record was soft-deleted
        bool emailExists = await context.Doctors
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Email == request.Email, cancellationToken);

        if (emailExists)
            return Result<DoctorResponse>.Failure("Email already registered", 400);

        var doctor = new Doctor
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Specialization = request.Specialization,
            YearsOfExperience = request.YearsOfExperience,
            Bio = request.Bio,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Doctors.Add(doctor);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<DoctorResponse>.Failure("Email already registered", 400);
        }

        return Result<DoctorResponse>.Success(doctor.ToResponse());
    }

    public async Task<Result<DoctorResponse>> UpdateAsync(
        int id, UpdateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        bool allFieldsNull = request.FullName is null
            && request.Email is null
            && request.Phone is null
            && request.Specialization is null
            && request.YearsOfExperience is null
            && request.Bio is null
            && request.IsAvailable is null;

        if (allFieldsNull)
            return Result<DoctorResponse>.Failure("At least one field must be provided", 400);

        Doctor? doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (doctor is null)
            return Result<DoctorResponse>.Failure("Doctor not found", 404);

        // IgnoreQueryFilters: prevent email reuse even if prior record was soft-deleted
        if (request.Email is not null && request.Email != doctor.Email)
        {
            bool emailExists = await context.Doctors
                .IgnoreQueryFilters()
                .AnyAsync(d => d.Email == request.Email && d.Id != id, cancellationToken);

            if (emailExists)
                return Result<DoctorResponse>.Failure("Email already registered", 400);
        }

        if (request.FullName is not null) doctor.FullName = request.FullName;
        if (request.Email is not null) doctor.Email = request.Email;
        if (request.Phone is not null) doctor.Phone = request.Phone;
        if (request.Specialization is not null) doctor.Specialization = request.Specialization;
        if (request.YearsOfExperience is not null) doctor.YearsOfExperience = request.YearsOfExperience.Value;
        if (request.Bio is not null) doctor.Bio = request.Bio;
        if (request.IsAvailable is not null) doctor.IsAvailable = request.IsAvailable.Value;

        doctor.UpdatedAt = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<DoctorResponse>.Failure("Email already registered", 400);
        }

        return Result<DoctorResponse>.Success(doctor.ToResponse());
    }

    public async Task<Result<bool>> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        Doctor? doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (doctor is null)
            return Result<bool>.Failure("Doctor not found", 404);

        doctor.IsDeleted = true;
        doctor.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
