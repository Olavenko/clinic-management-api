using Microsoft.EntityFrameworkCore;

using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Appointments;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Services;

public class AppointmentService(AppDbContext context) : IAppointmentService
{
    public async Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(
        AppointmentFilterRequest filter, CancellationToken cancellationToken = default)
    {

        IQueryable<Appointment> query = context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string searchTerm = filter.SearchTerm.Trim().ToLower();

            query = query.Where(a =>
                a.Patient.FullName.ToLower().Contains(searchTerm) ||
                a.Doctor.FullName.ToLower().Contains(searchTerm));
        }

        if (filter.DateFrom is not null)
            query = query.Where(a => a.AppointmentDate >= filter.DateFrom.Value);

        if (filter.DateTo is not null)
            query = query.Where(a => a.AppointmentDate <= filter.DateTo.Value);

        if (filter.Status is not null)
            query = query.Where(a => a.Status == filter.Status.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Appointment> appointments = await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<AppointmentResponse>
        {
            Items = appointments.Select(a => a.ToResponse()),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return Result<PagedResponse<AppointmentResponse>>.Success(response);
    }

    public async Task<Result<PagedResponse<AppointmentResponse>>> GetByPatientAsync(
        int patientId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {

        // Global Query Filter ensures soft-deleted patients return null
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient is null)
            return Result<PagedResponse<AppointmentResponse>>.Failure("Patient not found", 404);

        IQueryable<Appointment> query = context.Appointments
            .Where(a => a.PatientId == patientId)
            .Include(a => a.Patient)
            .Include(a => a.Doctor);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Appointment> appointments = await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<AppointmentResponse>
        {
            Items = appointments.Select(a => a.ToResponse()),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Result<PagedResponse<AppointmentResponse>>.Success(response);
    }

    public async Task<Result<PagedResponse<AppointmentResponse>>> GetByDoctorAsync(
        int doctorId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        // Global Query Filter ensures soft-deleted doctors return null
        Doctor? doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);

        if (doctor is null)
            return Result<PagedResponse<AppointmentResponse>>.Failure("Doctor not found", 404);

        IQueryable<Appointment> query = context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Include(a => a.Patient)
            .Include(a => a.Doctor);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Appointment> appointments = await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<AppointmentResponse>
        {
            Items = appointments.Select(a => a.ToResponse()),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Result<PagedResponse<AppointmentResponse>>.Success(response);
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        Appointment? appointment = await context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return appointment is null
            ? Result<AppointmentResponse>.Failure("Appointment not found", 404)
            : Result<AppointmentResponse>.Success(appointment.ToResponse());
    }

    public async Task<Result<AppointmentResponse>> CreateAsync(
    CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        // Rule 1 — Patient must exist
        Patient? patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient is null)
            return Result<AppointmentResponse>.Failure("Patient not found", 404);

        // Rule 2 — Doctor must exist and be available
        Doctor? doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);

        if (doctor is null)
            return Result<AppointmentResponse>.Failure("Doctor not found", 404);

        if (!doctor.IsAvailable)
            return Result<AppointmentResponse>.Failure("Doctor is not available", 400);

        // Rule 3 — Appointment cannot be in the past
        if (request.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<AppointmentResponse>.Failure("Appointment date cannot be in the past", 400);

        // Serializable transaction: overlap check + save are atomic
        // Prevents two concurrent requests from booking the same slot
        using var transaction = await context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Overlap detection values
            TimeOnly newStart = request.AppointmentTime;
            TimeOnly newEnd = request.AppointmentTime.AddMinutes(request.DurationMinutes);

            // Rule 4 — Patient overlap check
            bool patientConflict = await context.Appointments
                .Where(a => a.PatientId == request.PatientId
                    && a.AppointmentDate == request.AppointmentDate
                    && a.Status == AppointmentStatus.Scheduled)
                .AnyAsync(a =>
                    a.AppointmentTime < newEnd
                    && newStart < a.AppointmentTime.AddMinutes(a.DurationMinutes),
                    cancellationToken);

            if (patientConflict)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AppointmentResponse>.Failure(
                    "Patient already has an appointment at this time", 400);
            }

            // Rule 5 — Doctor overlap check
            bool doctorConflict = await context.Appointments
                .Where(a => a.DoctorId == request.DoctorId
                    && a.AppointmentDate == request.AppointmentDate
                    && a.Status == AppointmentStatus.Scheduled)
                .AnyAsync(a =>
                    a.AppointmentTime < newEnd
                    && newStart < a.AppointmentTime.AddMinutes(a.DurationMinutes),
                    cancellationToken);

            if (doctorConflict)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AppointmentResponse>.Failure(
                    "Doctor already has an appointment at this time", 400);
            }

            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                AppointmentDate = request.AppointmentDate,
                AppointmentTime = request.AppointmentTime,
                DurationMinutes = request.DurationMinutes,
                Notes = request.Notes,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Load navigation properties for response
            appointment.Patient = patient;
            appointment.Doctor = doctor;

            return Result<AppointmentResponse>.Success(appointment.ToResponse());
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AppointmentResponse>.Failure(
                "Appointment conflict detected, please try again", 409);
        }
    }

    public async Task<Result<AppointmentResponse>> UpdateAsync(
        int id, UpdateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        // Rule 1 — At least one field must be provided
        bool allFieldsNull = request.AppointmentDate is null
            && request.AppointmentTime is null
            && request.DurationMinutes is null
            && request.Notes is null;

        if (allFieldsNull)
            return Result<AppointmentResponse>.Failure("At least one field must be provided", 400);

        // Rule 2 — Appointment must exist
        Appointment? appointment = await context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment is null)
            return Result<AppointmentResponse>.Failure("Appointment not found", 404);

        // Rule 3 — Only Scheduled appointments can be updated
        if (appointment.Status != AppointmentStatus.Scheduled)
            return Result<AppointmentResponse>.Failure("Only scheduled appointments can be updated", 400);

        // Rule 3.5 — Doctor must still be available for rescheduling
        if (!appointment.Doctor.IsAvailable)
            return Result<AppointmentResponse>.Failure("Doctor is not available", 400);

        // Rule 4 — New date cannot be in the past (only if date is being changed)
        if (request.AppointmentDate is not null
            && request.AppointmentDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<AppointmentResponse>.Failure("Appointment date cannot be in the past", 400);

        // Serializable transaction: overlap check + save are atomic
        using var transaction = await context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Determine final values for conflict check
            DateOnly finalDate = request.AppointmentDate ?? appointment.AppointmentDate;
            TimeOnly finalTime = request.AppointmentTime ?? appointment.AppointmentTime;
            int finalDuration = request.DurationMinutes ?? appointment.DurationMinutes;

            // Rule 5 — Check overlapping only if date, time, or duration changed
            bool scheduleChanged = request.AppointmentDate is not null
                || request.AppointmentTime is not null
                || request.DurationMinutes is not null;

            if (scheduleChanged)
            {
                TimeOnly newStart = finalTime;
                TimeOnly newEnd = finalTime.AddMinutes(finalDuration);

                // Patient conflict (exclude current appointment)
                bool patientConflict = await context.Appointments
                    .Where(a => a.Id != id
                        && a.PatientId == appointment.PatientId
                        && a.AppointmentDate == finalDate
                        && a.Status == AppointmentStatus.Scheduled)
                    .AnyAsync(a =>
                        a.AppointmentTime < newEnd
                        && newStart < a.AppointmentTime.AddMinutes(a.DurationMinutes),
                        cancellationToken);

                if (patientConflict)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<AppointmentResponse>.Failure(
                        "Patient already has an appointment at this time", 400);
                }

                // Doctor conflict (exclude current appointment)
                bool doctorConflict = await context.Appointments
                    .Where(a => a.Id != id
                        && a.DoctorId == appointment.DoctorId
                        && a.AppointmentDate == finalDate
                        && a.Status == AppointmentStatus.Scheduled)
                    .AnyAsync(a =>
                        a.AppointmentTime < newEnd
                        && newStart < a.AppointmentTime.AddMinutes(a.DurationMinutes),
                        cancellationToken);

                if (doctorConflict)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<AppointmentResponse>.Failure(
                        "Doctor already has an appointment at this time", 400);
                }
            }

            // Apply updates (inside transaction — atomic with overlap check)
            if (request.AppointmentDate is not null) appointment.AppointmentDate = request.AppointmentDate.Value;
            if (request.AppointmentTime is not null) appointment.AppointmentTime = request.AppointmentTime.Value;
            if (request.DurationMinutes is not null) appointment.DurationMinutes = request.DurationMinutes.Value;
            if (request.Notes is not null) appointment.Notes = request.Notes;

            appointment.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<AppointmentResponse>.Success(appointment.ToResponse());
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AppointmentResponse>.Failure(
                "Appointment conflict detected, please try again", 409);
        }
    }

    public async Task<Result<AppointmentResponse>> UpdateStatusAsync(
        int id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default)
    {
        // Rule 1 — Appointment must exist
        Appointment? appointment = await context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment is null)
            return Result<AppointmentResponse>.Failure("Appointment not found", 404);

        // Rule 2 — Status transition rules
        if (appointment.Status == AppointmentStatus.Completed)
            return Result<AppointmentResponse>.Failure("Completed appointments cannot be changed", 400);

        if (appointment.Status == AppointmentStatus.Cancelled)
            return Result<AppointmentResponse>.Failure("Cancelled appointments cannot be changed", 400);

        // Only Scheduled → Completed or Scheduled → Cancelled are valid
        if (request.Status != AppointmentStatus.Completed && request.Status != AppointmentStatus.Cancelled)
            return Result<AppointmentResponse>.Failure("Invalid status transition", 400);

        appointment.Status = request.Status;
        appointment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result<AppointmentResponse>.Success(appointment.ToResponse());
    }

    public async Task<Result<bool>> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        // Rule 1 — Appointment must exist
        Appointment? appointment = await context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (appointment is null)
            return Result<bool>.Failure("Appointment not found", 404);

        // Rule 2 — Only Cancelled appointments can be deleted
        if (appointment.Status != AppointmentStatus.Cancelled)
            return Result<bool>.Failure("Only cancelled appointments can be deleted", 400);

        // Hard delete — status lifecycle handles audit trail
        context.Appointments.Remove(appointment);
        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
