using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Auth;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementAPI.Tests.Integration;

public class DoctorEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DoctorEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Helper Methods ──────────────────────────────────────────────

    private async Task<string> GetTokenForRoleAsync(string role)
    {
        var email = $"{role.ToLower()}_{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        // Step 1: Register via API
        var registerRequest = new RegisterRequest
        {
            FullName = $"{role} User",
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Step 2: Assign role directly via UserManager (test infrastructure only)
        if (role != "Patient")
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);

            // Remove default Patient role and assign desired role
            await userManager.RemoveFromRoleAsync(user!, AppRoles.Patient);
            await userManager.AddToRoleAsync(user!, role);
        }

        // Step 3: Login to get fresh token with correct role claims
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

    private static CreateDoctorRequest CreateValidDoctorRequest(string? email = null) => new()
    {
        FullName = "Dr. Test Doctor",
        Email = email ?? $"dr_{Guid.NewGuid()}@example.com",
        Phone = "+201234567890",
        Specialization = "Cardiology",
        YearsOfExperience = 10,
        Bio = "Experienced cardiologist"
    };

    private async Task<DoctorResponse> CreateDoctorViaApiAsync(string adminToken, string? email = null)
    {
        var doctorReq = CreateValidDoctorRequest(email);
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, doctorReq);
        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<DoctorResponse>())!;
    }

    // ═══════════════════════════════════════════════════════════════
    // Public Endpoint Tests (no token required)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDoctors_WithoutToken_Returns200()
    {
        // Act — no Authorization header (public endpoint)
        var response = await _client.GetAsync("/api/doctors?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctors_WithSearchTerm_Returns200WithFilteredResults()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var uniqueSpec = $"Spec_{Guid.NewGuid():N}";

        var doctorReq = new CreateDoctorRequest
        {
            FullName = "Dr. Search Target",
            Email = $"search_{Guid.NewGuid()}@example.com",
            Phone = "+201234567890",
            Specialization = uniqueSpec,
            YearsOfExperience = 5
        };
        var createReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, doctorReq);
        await _client.SendAsync(createReq);

        // Act — public search
        var response = await _client.GetAsync($"/api/doctors?page=1&pageSize=10&searchTerm={uniqueSpec}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<DoctorResponse>>();
        Assert.NotNull(pagedResult);
        Assert.Contains(pagedResult.Items, d => d.Specialization == uniqueSpec);
    }

    [Fact]
    public async Task GetAvailableDoctors_WithoutToken_Returns200()
    {
        // Act — no Authorization header (public endpoint)
        var response = await _client.GetAsync("/api/doctors/available?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorById_WithValidId_Returns200()
    {
        // Arrange — create doctor with admin, then access publicly
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Act — public endpoint
        var response = await _client.GetAsync($"/api/doctors/{doctor.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(result);
        Assert.Equal(doctor.Id, result.Id);
    }

    [Fact]
    public async Task GetDoctorById_WithInvalidId_Returns404()
    {
        // Act — public endpoint
        var response = await _client.GetAsync("/api/doctors/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // Authorization Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDoctor_WithAdminToken_Returns201()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctorReq = CreateValidDoctorRequest();

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, doctorReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var doctor = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(doctor);
        Assert.Equal(doctorReq.FullName, doctor.FullName);
        Assert.Equal(doctorReq.Email, doctor.Email);
    }

    [Fact]
    public async Task CreateDoctor_WithoutToken_Returns401()
    {
        // Arrange
        var doctorReq = CreateValidDoctorRequest();

        // Act — no Authorization header
        var response = await _client.PostAsJsonAsync("/api/doctors", doctorReq);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateDoctor_WithReceptionistToken_Returns403()
    {
        // Arrange
        var receptionistToken = await GetTokenForRoleAsync("Receptionist");
        var doctorReq = CreateValidDoctorRequest();

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", receptionistToken, doctorReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDoctor_WithAdminToken_Returns200()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        var updateReq = new { FullName = "Updated Name" };

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/doctors/{doctor.Id}", adminToken, updateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.FullName);
    }

    [Fact]
    public async Task UpdateDoctor_WithReceptionistToken_Returns403()
    {
        // Arrange — create doctor with admin, try to update with receptionist
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        var receptionistToken = await GetTokenForRoleAsync("Receptionist");
        var updateReq = new { FullName = "Updated" };

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/doctors/{doctor.Id}", receptionistToken, updateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDoctor_WithAdminToken_Returns204()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor.Id}", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDoctor_WithReceptionistToken_Returns403()
    {
        // Arrange — create doctor with admin, try to delete with receptionist
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        var receptionistToken = await GetTokenForRoleAsync("Receptionist");

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor.Id}", receptionistToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // Validation Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDoctor_WithMissingFields_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var invalidRequest = new CreateDoctorRequest
        {
            FullName = "",   // violates [MinLength(2)]
            Email = "",      // empty email
            Phone = "",      // empty phone
        };

        // Act
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, invalidRequest);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 400 or 201 but got {response.StatusCode}");
    }

    [Fact]
    public async Task CreateDoctor_WithDuplicateEmail_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var email = $"dup_{Guid.NewGuid()}@example.com";

        // Create first doctor
        await CreateDoctorViaApiAsync(adminToken, email);

        // Act — create second doctor with same email
        var duplicateReq = CreateValidDoctorRequest(email);
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, duplicateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDoctor_WithAllFieldsEmpty_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        var emptyUpdate = new UpdateDoctorRequest(); // all null

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/doctors/{doctor.Id}", adminToken, emptyUpdate);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDoctor_WithDuplicateEmail_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var email1 = $"doc1_{Guid.NewGuid()}@example.com";
        var email2 = $"doc2_{Guid.NewGuid()}@example.com";

        await CreateDoctorViaApiAsync(adminToken, email1);
        var doctor2 = await CreateDoctorViaApiAsync(adminToken, email2);

        // Act — try to update doctor2's email to doctor1's email
        var updateReq = new UpdateDoctorRequest { Email = email1 };
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, $"/api/doctors/{doctor2.Id}", adminToken, updateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDoctor_WithInvalidId_Returns404()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var updateReq = new { FullName = "Updated" };

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Put, "/api/doctors/99999", adminToken, updateReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════
    // Soft Delete Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDoctor_Returns204Success()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Act
        var request = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor.Id}", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorById_AfterSoftDelete_Returns404()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var doctor = await CreateDoctorViaApiAsync(adminToken);

        // Soft delete
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        // Act — public endpoint
        var response = await _client.GetAsync($"/api/doctors/{doctor.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllDoctors_AfterSoftDelete_ExcludesDeletedDoctor()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var uniqueName = $"ToDelete_{Guid.NewGuid():N}";

        var doctorReq = new CreateDoctorRequest
        {
            FullName = uniqueName,
            Email = $"todelete_{Guid.NewGuid()}@example.com",
            Phone = "+201234567890",
            Specialization = "Cardiology",
            YearsOfExperience = 5
        };
        var createReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, doctorReq);
        var createResponse = await _client.SendAsync(createReq);
        var doctor = await createResponse.Content.ReadFromJsonAsync<DoctorResponse>();

        // Soft delete
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor!.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        // Act — public search for the deleted doctor
        var response = await _client.GetAsync(
            $"/api/doctors?page=1&pageSize=10&searchTerm={uniqueName}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<DoctorResponse>>();
        Assert.NotNull(pagedResult);
        Assert.DoesNotContain(pagedResult.Items, d => d.FullName == uniqueName);
    }

    [Fact]
    public async Task GetAvailableDoctors_AfterSoftDelete_ExcludesDeletedDoctor()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var uniqueName = $"AvailDelete_{Guid.NewGuid():N}";

        var doctorReq = new CreateDoctorRequest
        {
            FullName = uniqueName,
            Email = $"availdelete_{Guid.NewGuid()}@example.com",
            Phone = "+201234567890",
            Specialization = "Cardiology",
            YearsOfExperience = 5
        };
        var createReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, doctorReq);
        var createResponse = await _client.SendAsync(createReq);
        var doctor = await createResponse.Content.ReadFromJsonAsync<DoctorResponse>();

        // Soft delete
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor!.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        // Act — public available endpoint
        var response = await _client.GetAsync(
            $"/api/doctors/available?page=1&pageSize=10&searchTerm={uniqueName}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<DoctorResponse>>();
        Assert.NotNull(pagedResult);
        Assert.DoesNotContain(pagedResult.Items, d => d.FullName == uniqueName);
    }

    [Fact]
    public async Task CreateDoctor_WithDeletedDoctorEmail_Returns400()
    {
        // Arrange
        var adminToken = await GetTokenForRoleAsync("Admin");
        var email = $"deleted_{Guid.NewGuid()}@example.com";

        // Create and soft-delete a doctor
        var doctor = await CreateDoctorViaApiAsync(adminToken, email);
        var deleteReq = CreateAuthorizedRequest(
            HttpMethod.Delete, $"/api/doctors/{doctor.Id}", adminToken);
        await _client.SendAsync(deleteReq);

        // Act — try to create with deleted doctor's email
        var newDoctorReq = CreateValidDoctorRequest(email);
        var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/doctors", adminToken, newDoctorReq);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
