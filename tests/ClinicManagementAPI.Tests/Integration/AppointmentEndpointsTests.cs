using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Appointments;
using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementAPI.Tests.Integration;

public class AppointmentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Helper Methods ──────────────────────────────────────────────

    private async Task<string> GetTokenForRoleAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        var registerRequest = new RegisterRequest
        {
            FullName = $"{role} User",
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        if (role != "Patient")
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);

            await userManager.RemoveFromRoleAsync(user!, AppRoles.Patient);
            await userManager.AddToRoleAsync(user!, role);
        }

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return authData!.AccessToken;
    }

    private HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    private async Task<DoctorResponse> CreateDoctorViaApiAsync(string adminToken, string? email = null)
    {
        var doctorReq = new CreateDoctorRequest
        {
            FullName = "Dr. Test Doctor",
            Email = email ?? $"dr_{Guid.NewGuid()}@example.com",
            Phone = "+201234567890",
            Specialization = "Cardiology",
            YearsOfExperience = 10,
            Bio = "Experienced cardiologist"
        };
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, doctorReq);
        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<DoctorResponse>())!;
    }

    private async Task<PatientResponse> CreatePatientViaApiAsync(string token, string? email = null)
    {
        var patientReq = new CreatePatientRequest
        {
            FullName = "Test Patient",
            Email = email ?? $"patient_{Guid.NewGuid()}@example.com",
            Phone = "+201234567890",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        };
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/patients", token, patientReq);
        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<PatientResponse>())!;
    }

    private async Task<AppointmentResponse> CreateAppointmentViaApiAsync(
        string token, int patientId, int doctorId,
        DateOnly? date = null, TimeOnly? time = null)
    {
        var apptReq = new CreateAppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AppointmentTime = time ?? new TimeOnly(10, 0),
            DurationMinutes = 30,
            Notes = "Test appointment"
        };
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", token, apptReq);
        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<AppointmentResponse>())!;
    }

    // ═══════════════════════════════════════════════════════════════
    // Authorization Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAppointments_WithAdminToken_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/appointments?page=1&pageSize=10", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointments_WithReceptionistToken_Returns200()
    {
        // Arrange
        var receptionistToken = await GetTokenForRoleAsync("Receptionist");

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/appointments?page=1&pageSize=10", receptionistToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointments_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/appointments?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointments_WithPatientToken_Returns403()
    {
        // Arrange
        var patientToken = await GetTokenForRoleAsync("Patient");

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/appointments?page=1&pageSize=10", patientToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAppointment_WithAdminToken_Returns204()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Cancel first
        var cancelReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Cancelled });
        await _client.SendAsync(cancelReq);

        // Act — delete
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/appointments/{appt.Id}", adminToken);
        var response = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAppointment_WithReceptionistToken_Returns403()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var receptionistToken = await GetTokenForRoleAsync("Receptionist");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Cancel first
        var cancelReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Cancelled });
        await _client.SendAsync(cancelReq);

        // Act — try to delete with receptionist
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/appointments/{appt.Id}", receptionistToken);
        var response = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // Filter Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAppointments_WithSearchTerm_Returns200WithFilteredResults()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var uniqueName = $"SearchPatient_{Guid.NewGuid():N}";

        var patientReq = new CreatePatientRequest
        {
            FullName = uniqueName,
            Email = $"search_{Guid.NewGuid()}@example.com",
            Phone = "+201234567890",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        };
        var createPatientReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/patients", adminToken, patientReq);
        var patientResponse = await _client.SendAsync(createPatientReq);
        var patient = await patientResponse.Content.ReadFromJsonAsync<PatientResponse>();

        var doctor = await CreateDoctorViaApiAsync(adminToken);
        await CreateAppointmentViaApiAsync(adminToken, patient!.Id, doctor.Id);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Get, $"/api/appointments?page=1&pageSize=10&searchTerm={uniqueName}", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<AppointmentResponse>>();
        Assert.NotNull(pagedResult);
        Assert.Contains(pagedResult.Items, a => a.PatientName == uniqueName);
    }

    [Fact]
    public async Task GetAppointments_WithDateRange_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");

        // Act
        var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd");
        var dateTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd");
        var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/appointments?page=1&pageSize=10&dateFrom={dateFrom}&dateTo={dateTo}",
            adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointments_WithStatusFilter_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Get, "/api/appointments?page=1&pageSize=10&status=0", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByPatient_WithValidPatient_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Get, $"/api/appointments/patient/{patient.Id}?page=1&pageSize=10", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<AppointmentResponse>>();
        Assert.NotNull(pagedResult);
        Assert.Contains(pagedResult.Items, a => a.PatientId == patient.Id);
    }

    [Fact]
    public async Task GetByPatient_WithSoftDeletedPatient_Returns404()
    {
        // Arrange — create patient, create appointment, soft-delete patient
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);

        // Soft delete patient
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/patients/{patient.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Get, $"/api/appointments/patient/{patient.Id}?page=1&pageSize=10", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByDoctor_WithValidDoctor_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Get, $"/api/appointments/doctor/{doctor.Id}?page=1&pageSize=10", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<AppointmentResponse>>();
        Assert.NotNull(pagedResult);
        Assert.Contains(pagedResult.Items, a => a.DoctorId == doctor.Id);
    }

    [Fact]
    public async Task GetByDoctor_WithSoftDeletedDoctor_Returns404()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Soft delete doctor
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Get, $"/api/appointments/doctor/{doctor.Id}?page=1&pageSize=10", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // Business Rule Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAppointment_WithValidData_Returns201()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        var apptReq = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AppointmentTime = new TimeOnly(14, 0),
            DurationMinutes = 30,
            Notes = "Test"
        };

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", adminToken, apptReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var appt = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(appt);
        Assert.Equal("Scheduled", appt.Status);
    }

    [Fact]
    public async Task CreateAppointment_WithPastDate_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        var apptReq = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            AppointmentTime = new TimeOnly(10, 0),
            DurationMinutes = 30
        };

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", adminToken, apptReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithUnavailableDoctor_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Make doctor unavailable
        var updateReq = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/doctors/{doctor.Id}", adminToken,
            new { IsAvailable = false });
        await _client.SendAsync(updateReq);

        var apptReq = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AppointmentTime = new TimeOnly(10, 0),
            DurationMinutes = 30
        };

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", adminToken, apptReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithSoftDeletedPatient_Returns400or404()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Soft delete patient
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/patients/{patient.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        var apptReq = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AppointmentTime = new TimeOnly(10, 0),
            DurationMinutes = 30
        };

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", adminToken, apptReq);
        var response = await _client.SendAsync(request);

        // Assert — 404 (Patient not found because of soft-delete filter)
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAppointment_WithPatientConflict_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor1 = await CreateDoctorViaApiAsync(adminToken);
        var doctor2 = await CreateDoctorViaApiAsync(adminToken);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Book first appointment at 10:00
        await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor1.Id, futureDate, new TimeOnly(10, 0));

        // Act — try to book same patient at 10:15 with different doctor
        var conflictReq = new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            DoctorId = doctor2.Id,
            AppointmentDate = futureDate,
            AppointmentTime = new TimeOnly(10, 15),
            DurationMinutes = 30
        };
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", adminToken, conflictReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithDoctorConflict_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient1 = await CreatePatientViaApiAsync(adminToken);
        var patient2 = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Book first appointment at 10:00
        await CreateAppointmentViaApiAsync(adminToken, patient1.Id, doctor.Id, futureDate, new TimeOnly(10, 0));

        // Act — try to book same doctor at 10:15 with different patient
        var conflictReq = new CreateAppointmentRequest
        {
            PatientId = patient2.Id,
            DoctorId = doctor.Id,
            AppointmentDate = futureDate,
            AppointmentTime = new TimeOnly(10, 15),
            DurationMinutes = 30
        };
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/appointments", adminToken, conflictReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAppointment_WithValidData_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        var updateReq = new { Notes = "Updated notes" };

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/appointments/{appt.Id}", adminToken, updateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.Equal("Updated notes", updated!.Notes);
    }

    [Fact]
    public async Task UpdateAppointment_WithEmptyRequest_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        var emptyUpdate = new UpdateAppointmentRequest(); // all null

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/appointments/{appt.Id}", adminToken, emptyUpdate);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAppointment_WithCompletedAppointment_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Complete it
        var statusReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Completed });
        await _client.SendAsync(statusReq);

        // Act — try to update
        var updateReq = new { Notes = "Updated" };
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/appointments/{appt.Id}", adminToken, updateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ScheduledToCompleted_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Act
        var statusReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Completed });
        var response = await _client.SendAsync(statusReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.Equal("Completed", updated!.Status);
    }

    [Fact]
    public async Task UpdateStatus_ScheduledToCancelled_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Act
        var statusReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Cancelled });
        var response = await _client.SendAsync(statusReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.Equal("Cancelled", updated!.Status);
    }

    [Fact]
    public async Task UpdateStatus_CompletedToCancelled_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Complete first
        var completeReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Completed });
        await _client.SendAsync(completeReq);

        // Act — try to cancel completed
        var cancelReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Cancelled });
        var response = await _client.SendAsync(cancelReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAppointment_WithCancelledAppointment_Returns204()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Cancel first
        var cancelReq = CreateAuthorizedRequest(
            HttpMethod.Patch, $"/api/appointments/{appt.Id}/status", adminToken,
            new { Status = (int)AppointmentStatus.Cancelled });
        await _client.SendAsync(cancelReq);

        // Act
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/appointments/{appt.Id}", adminToken);
        var response = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAppointment_WithScheduledAppointment_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var patient = await CreatePatientViaApiAsync(adminToken);
        var doctor = await CreateDoctorViaApiAsync(adminToken);
        var appt = await CreateAppointmentViaApiAsync(adminToken, patient.Id, doctor.Id);

        // Act — try to delete without cancelling first
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/appointments/{appt.Id}", adminToken);
        var response = await _client.SendAsync(deleteReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
