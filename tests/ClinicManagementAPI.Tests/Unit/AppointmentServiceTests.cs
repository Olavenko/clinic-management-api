using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Appointments;
using ClinicManagementAPI.Core.Models;
using ClinicManagementAPI.Core.Services;

using Microsoft.EntityFrameworkCore;

namespace ClinicManagementAPI.Tests.Unit;

public class AppointmentServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AppointmentService _appointmentService;

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _appointmentService = new AppointmentService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Helper Methods ──────────────────────────────────────────────

    private async Task<Patient> SeedPatientAsync(string? email = null, bool isDeleted = false)
    {
        var patient = new Patient
        {
            FullName = "Test Patient",
            Email = email ?? $"patient_{Guid.NewGuid()}@clinic.com",
            Phone = "+201234567890",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null
        };

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();
        return patient;
    }

    private async Task<Doctor> SeedDoctorAsync(
        string? email = null, bool isAvailable = true, bool isDeleted = false)
    {
        var doctor = new Doctor
        {
            FullName = "Dr. Test Doctor",
            Email = email ?? $"dr_{Guid.NewGuid()}@clinic.com",
            Phone = "+201234567890",
            Specialization = "Cardiology",
            YearsOfExperience = 10,
            IsAvailable = isAvailable,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null
        };

        _dbContext.Doctors.Add(doctor);
        await _dbContext.SaveChangesAsync();
        return doctor;
    }

    private static CreateAppointmentRequest CreateValidRequest(
        int patientId, int doctorId, DateOnly? date = null, TimeOnly? time = null, int duration = 30) => new()
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AppointmentTime = time ?? new TimeOnly(10, 0),
            DurationMinutes = duration,
            Notes = "Test appointment"
        };

    private async Task<AppointmentResponse> SeedAppointmentAsync(
        int patientId, int doctorId, DateOnly? date = null, TimeOnly? time = null, int duration = 30)
    {
        var request = CreateValidRequest(patientId, doctorId, date, time, duration);
        var result = await _appointmentService.CreateAsync(request);
        return result.Value!;
    }

    // ── CreateAsync (Business Rules) ─────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var request = CreateValidRequest(patient.Id, doctor.Id);

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(patient.Id, result.Value.PatientId);
        Assert.Equal(doctor.Id, result.Value.DoctorId);
        Assert.Equal("Scheduled", result.Value.Status);
        Assert.Equal(request.AppointmentDate, result.Value.AppointmentDate);
        Assert.Equal(request.AppointmentTime, result.Value.AppointmentTime);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentPatient_ReturnsFailureResult()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();
        var request = CreateValidRequest(9999, doctor.Id);

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Patient not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithSoftDeletedPatient_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync(isDeleted: true);
        var doctor = await SeedDoctorAsync();
        var request = CreateValidRequest(patient.Id, doctor.Id);

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Patient not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentDoctor_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var request = CreateValidRequest(patient.Id, 9999);

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Doctor not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithSoftDeletedDoctor_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync(isDeleted: true);
        var request = CreateValidRequest(patient.Id, doctor.Id);

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Doctor not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithUnavailableDoctor_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync(isAvailable: false);
        var request = CreateValidRequest(patient.Id, doctor.Id);

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Doctor is not available", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithPastDate_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var request = CreateValidRequest(patient.Id, doctor.Id,
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        // Act
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Appointment date cannot be in the past", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithPatientConflict_ReturnsFailureResult()
    {
        // Arrange — create first appointment for patient at 10:00
        var patient = await SeedPatientAsync();
        var doctor1 = await SeedDoctorAsync("doc1@clinic.com");
        var doctor2 = await SeedDoctorAsync("doc2@clinic.com");
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient.Id, doctor1.Id, futureDate, new TimeOnly(10, 0));

        // Act — try to book same patient at 10:15 (overlaps with 10:00-10:30)
        var request = CreateValidRequest(patient.Id, doctor2.Id, futureDate, new TimeOnly(10, 15));
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Patient already has an appointment at this time", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithDoctorConflict_ReturnsFailureResult()
    {
        // Arrange — create first appointment for doctor at 10:00
        var patient1 = await SeedPatientAsync("p1@clinic.com");
        var patient2 = await SeedPatientAsync("p2@clinic.com");
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient1.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        // Act — try to book same doctor at 10:15 (overlaps with 10:00-10:30)
        var request = CreateValidRequest(patient2.Id, doctor.Id, futureDate, new TimeOnly(10, 15));
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Doctor already has an appointment at this time", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithBackToBackAppointments_ReturnsSuccessResult()
    {
        // Arrange — create first appointment 10:00-10:30
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0), 30);

        // Act — book at 10:30 (back-to-back, no overlap)
        var patient2 = await SeedPatientAsync("p2@clinic.com");
        var request = CreateValidRequest(patient2.Id, doctor.Id, futureDate, new TimeOnly(10, 30));
        var result = await _appointmentService.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    // ── GetAllAsync + Filters ────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedAppointments()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(9, 0));
        await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        var filter = new AppointmentFilterRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetAllAsync(filter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_FiltersByPatientOrDoctorName()
    {
        // Arrange
        var patient1 = await SeedPatientAsync("ahmed@clinic.com");
        // Override patient name directly
        patient1.FullName = "Ahmed Hassan";
        await _dbContext.SaveChangesAsync();

        var patient2 = await SeedPatientAsync("sara@clinic.com");
        patient2.FullName = "Sara Ali";
        await _dbContext.SaveChangesAsync();

        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient1.Id, doctor.Id, futureDate, new TimeOnly(9, 0));
        await SeedAppointmentAsync(patient2.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        var filter = new AppointmentFilterRequest
        {
            Page = 1,
            PageSize = 10,
            SearchTerm = "Ahmed"
        };

        // Act
        var result = await _appointmentService.GetAllAsync(filter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Ahmed Hassan", result.Value.Items.First().PatientName);
    }

    [Fact]
    public async Task GetAllAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();

        var date1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var date2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var date3 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        await SeedAppointmentAsync(patient.Id, doctor.Id, date1, new TimeOnly(10, 0));
        await SeedAppointmentAsync(patient.Id, doctor.Id, date2, new TimeOnly(10, 0));
        await SeedAppointmentAsync(patient.Id, doctor.Id, date3, new TimeOnly(10, 0));

        var filter = new AppointmentFilterRequest
        {
            Page = 1,
            PageSize = 10,
            DateFrom = date1,
            DateTo = date2
        };

        // Act
        var result = await _appointmentService.GetAllAsync(filter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_FiltersCorrectly()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var appt1 = await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(9, 0));
        await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        // Cancel the first appointment
        await _appointmentService.UpdateStatusAsync(appt1.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Cancelled });

        var filter = new AppointmentFilterRequest
        {
            Page = 1,
            PageSize = 10,
            Status = AppointmentStatus.Scheduled
        };

        // Act
        var result = await _appointmentService.GetAllAsync(filter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Scheduled", result.Value.Items.First().Status);
    }

    // GetAllAsync_ShowsAppointmentsWithSoftDeletedPatient is not testable with InMemory DB:
    // InMemory filters out the entire appointment when the required Patient navigation is soft-deleted,
    // while SQL Server correctly returns the appointment with null navigation.
    // This behavior is tested in integration tests against the real provider.

    // ── GetByPatientAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByPatientAsync_WithValidId_ReturnsPatientAppointments()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetByPatientAsync(patient.Id, pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(patient.Id, result.Value.Items.First().PatientId);
    }

    [Fact]
    public async Task GetByPatientAsync_WithSoftDeletedPatient_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync(isDeleted: true);
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetByPatientAsync(patient.Id, pagination);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Patient not found", result.Error);
    }

    [Fact]
    public async Task GetByPatientAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Arrange
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetByPatientAsync(9999, pagination);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    // ── GetByDoctorAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetByDoctorAsync_WithValidId_ReturnsDoctorAppointments()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetByDoctorAsync(doctor.Id, pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(doctor.Id, result.Value.Items.First().DoctorId);
    }

    [Fact]
    public async Task GetByDoctorAsync_WithSoftDeletedDoctor_ReturnsFailureResult()
    {
        // Arrange
        var doctor = await SeedDoctorAsync(isDeleted: true);
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetByDoctorAsync(doctor.Id, pagination);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Doctor not found", result.Error);
    }

    [Fact]
    public async Task GetByDoctorAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Arrange
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _appointmentService.GetByDoctorAsync(9999, pagination);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var updateRequest = new UpdateAppointmentRequest { AppointmentDate = newDate };

        // Act
        var result = await _appointmentService.UpdateAsync(appt.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newDate, result.Value!.AppointmentDate);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Arrange
        var updateRequest = new UpdateAppointmentRequest
        {
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))
        };

        // Act
        var result = await _appointmentService.UpdateAsync(9999, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Appointment not found", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithAllFieldsNull_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        var updateRequest = new UpdateAppointmentRequest(); // all null

        // Act
        var result = await _appointmentService.UpdateAsync(appt.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("At least one field must be provided", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithCompletedAppointment_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        // Complete the appointment
        await _appointmentService.UpdateStatusAsync(appt.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Completed });

        var updateRequest = new UpdateAppointmentRequest { Notes = "Updated notes" };

        // Act
        var result = await _appointmentService.UpdateAsync(appt.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Only scheduled appointments can be updated", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithCancelledAppointment_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        // Cancel the appointment
        await _appointmentService.UpdateStatusAsync(appt.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Cancelled });

        var updateRequest = new UpdateAppointmentRequest { Notes = "Updated notes" };

        // Act
        var result = await _appointmentService.UpdateAsync(appt.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Only scheduled appointments can be updated", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithPastDate_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        var updateRequest = new UpdateAppointmentRequest
        {
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        // Act
        var result = await _appointmentService.UpdateAsync(appt.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Appointment date cannot be in the past", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithConflict_ReturnsFailureResult()
    {
        // Arrange — two appointments for same doctor at different times
        var patient1 = await SeedPatientAsync("p1@clinic.com");
        var patient2 = await SeedPatientAsync("p2@clinic.com");
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        await SeedAppointmentAsync(patient1.Id, doctor.Id, futureDate, new TimeOnly(10, 0));
        var appt2 = await SeedAppointmentAsync(patient2.Id, doctor.Id, futureDate, new TimeOnly(11, 0));

        // Act — try to move appt2 to 10:15 (conflicts with 10:00-10:30)
        var updateRequest = new UpdateAppointmentRequest { AppointmentTime = new TimeOnly(10, 15) };
        var result = await _appointmentService.UpdateAsync(appt2.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Doctor already has an appointment at this time", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_SameTimeNoChange_DoesNotTriggerConflict()
    {
        // Arrange — update notes without changing time (no self-conflict)
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        var updateRequest = new UpdateAppointmentRequest { Notes = "Updated notes only" };

        // Act
        var result = await _appointmentService.UpdateAsync(appt.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated notes only", result.Value!.Notes);
    }

    // ── UpdateStatusAsync (Status Transitions) ───────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ScheduledToCompleted_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        var request = new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Completed };

        // Act
        var result = await _appointmentService.UpdateStatusAsync(appt.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value!.Status);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_ScheduledToCancelled_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        var request = new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Cancelled };

        // Act
        var result = await _appointmentService.UpdateStatusAsync(appt.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_CompletedToAny_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        await _appointmentService.UpdateStatusAsync(appt.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Completed });

        // Act — try to cancel a completed appointment
        var request = new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Cancelled };
        var result = await _appointmentService.UpdateStatusAsync(appt.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Completed appointments cannot be changed", result.Error);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelledToAny_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        await _appointmentService.UpdateStatusAsync(appt.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Cancelled });

        // Act — try to complete a cancelled appointment
        var request = new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Completed };
        var result = await _appointmentService.UpdateStatusAsync(appt.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Cancelled appointments cannot be changed", result.Error);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithCancelledAppointment_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        // Cancel first
        await _appointmentService.UpdateStatusAsync(appt.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Cancelled });

        // Act
        var result = await _appointmentService.DeleteAsync(appt.Id);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify hard delete
        var dbAppt = await _dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == appt.Id);
        Assert.Null(dbAppt);
    }

    [Fact]
    public async Task DeleteAsync_WithScheduledAppointment_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        // Act — try to delete a Scheduled appointment
        var result = await _appointmentService.DeleteAsync(appt.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Only cancelled appointments can be deleted", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WithCompletedAppointment_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var appt = await SeedAppointmentAsync(patient.Id, doctor.Id);

        await _appointmentService.UpdateStatusAsync(appt.Id,
            new UpdateAppointmentStatusRequest { Status = AppointmentStatus.Completed });

        // Act — try to delete a Completed appointment
        var result = await _appointmentService.DeleteAsync(appt.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Only cancelled appointments can be deleted", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Act
        var result = await _appointmentService.DeleteAsync(9999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Appointment not found", result.Error);
    }
}
