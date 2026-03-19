using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Models;
using ClinicManagementAPI.Core.Services;

using Microsoft.EntityFrameworkCore;

namespace ClinicManagementAPI.Tests.Unit;

public class PatientServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly PatientService _patientService;

    public PatientServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _patientService = new PatientService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Helper Methods ──────────────────────────────────────────────

    private static CreatePatientRequest CreateValidRequest(string? email = null) => new()
    {
        FullName = "Ahmed Hassan",
        Email = email ?? $"ahmed_{Guid.NewGuid()}@clinic.com",
        Phone = "+201234567890",
        Gender = Gender.Male,
        DateOfBirth = new DateOnly(1990, 5, 15),
        Address = "123 Main St, Cairo"
    };

    private async Task<PatientResponse> SeedPatientAsync(string? email = null)
    {
        var request = CreateValidRequest(email);
        var result = await _patientService.CreateAsync(request);
        return result.Value!;
    }

    private async Task SeedMultiplePatientsAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _patientService.CreateAsync(new CreatePatientRequest
            {
                FullName = $"Patient {i:D2}",
                Email = $"patient{i}@clinic.com",
                Phone = $"+2012345678{i:D2}",
                Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                DateOfBirth = new DateOnly(1990 + i, 1, 1),
                Address = $"Address {i}"
            });
        }
    }

    // ── GetAll + Search ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedPatients()
    {
        // Arrange
        await SeedMultiplePatientsAsync(5);
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _patientService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithPage2_ReturnsCorrectPatients()
    {
        // Arrange — seed 15 patients, request page 2 with pageSize 10
        await SeedMultiplePatientsAsync(15);
        var pagination = new PaginationRequest { Page = 2, PageSize = 10 };

        // Act
        var result = await _patientService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Items.Count()); // 15 - 10 = 5 on page 2
        Assert.Equal(2, result.Value.Page);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_FiltersCorrectly()
    {
        // Arrange — seed patients with distinct names
        await _patientService.CreateAsync(new CreatePatientRequest
        {
            FullName = "Ahmed Mohamed",
            Email = "ahmed@clinic.com",
            Phone = "+201111111111",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1990, 1, 1)
        });
        await _patientService.CreateAsync(new CreatePatientRequest
        {
            FullName = "Sara Ali",
            Email = "sara@clinic.com",
            Phone = "+201222222222",
            Gender = Gender.Female,
            DateOfBirth = new DateOnly(1995, 6, 1)
        });

        var pagination = new PaginationRequest { Page = 1, PageSize = 10, SearchTerm = "Ahmed" };

        // Act
        var result = await _patientService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Ahmed Mohamed", result.Value.Items.First().FullName);
    }

    [Fact]
    public async Task GetAllAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange — seed patients but search for nonexistent term
        await SeedPatientAsync();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10, SearchTerm = "NonExistent" };

        // Act
        var result = await _patientService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotReturnDeletedPatients()
    {
        // Arrange — create and then soft-delete a patient
        var patient = await SeedPatientAsync();
        await _patientService.DeleteAsync(patient.Id);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _patientService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    // ── GetById ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();

        // Act
        var result = await _patientService.GetByIdAsync(patient.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(patient.Id, result.Value!.Id);
        Assert.Equal(patient.FullName, result.Value.FullName);
        Assert.Equal(patient.Email, result.Value.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Act
        var result = await _patientService.GetByIdAsync(9999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Patient not found", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedPatient_ReturnsFailureResult()
    {
        // Arrange — create and soft-delete a patient
        var patient = await SeedPatientAsync();
        await _patientService.DeleteAsync(patient.Id);

        // Act
        var result = await _patientService.GetByIdAsync(patient.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    // ── Create ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _patientService.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.FullName, result.Value.FullName);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(request.Phone, result.Value.Phone);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsFailureResult()
    {
        // Arrange — create a patient first
        var email = "duplicate@clinic.com";
        await SeedPatientAsync(email);

        var duplicateRequest = CreateValidRequest(email);

        // Act
        var result = await _patientService.CreateAsync(duplicateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithDeletedPatientEmail_ReturnsFailureResult()
    {
        // Arrange — create and soft-delete a patient
        var email = "deleted@clinic.com";
        var patient = await SeedPatientAsync(email);
        await _patientService.DeleteAsync(patient.Id);

        // Act — try to create with same email (should fail: IgnoreQueryFilters check)
        var request = CreateValidRequest(email);
        var result = await _patientService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    // ── Update ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithValidId_ReturnsSuccessResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var updateRequest = new UpdatePatientRequest { FullName = "Updated Name" };

        // Act
        var result = await _patientService.UpdateAsync(patient.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value!.FullName);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Arrange
        var updateRequest = new UpdatePatientRequest { FullName = "Updated" };

        // Act
        var result = await _patientService.UpdateAsync(9999, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Patient not found", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithAllFieldsNull_ReturnsFailureResult()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var updateRequest = new UpdatePatientRequest(); // all fields null

        // Act
        var result = await _patientService.UpdateAsync(patient.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("At least one field must be provided", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ReturnsFailureResult()
    {
        // Arrange — create two patients
        var patient1 = await SeedPatientAsync("patient1@clinic.com");
        var patient2 = await SeedPatientAsync("patient2@clinic.com");

        // Try to update patient2's email to patient1's email
        var updateRequest = new UpdatePatientRequest { Email = "patient1@clinic.com" };

        // Act
        var result = await _patientService.UpdateAsync(patient2.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    // ── Delete (Soft Delete) ────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithValidId_SetsIsDeletedTrue()
    {
        // Arrange
        var patient = await SeedPatientAsync();

        // Act
        var result = await _patientService.DeleteAsync(patient.Id);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify in DB (bypass query filter)
        var dbPatient = await _dbContext.Patients
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == patient.Id);
        Assert.True(dbPatient.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_SetsDeletedAtToUtcNow()
    {
        // Arrange
        var patient = await SeedPatientAsync();
        var beforeDelete = DateTime.UtcNow;

        // Act
        var result = await _patientService.DeleteAsync(patient.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var dbPatient = await _dbContext.Patients
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == patient.Id);
        Assert.NotNull(dbPatient.DeletedAt);
        Assert.True(dbPatient.DeletedAt >= beforeDelete);
        Assert.True(dbPatient.DeletedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Act
        var result = await _patientService.DeleteAsync(9999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Patient not found", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_PatientDisappearsFromGetAll()
    {
        // Arrange — create two patients, delete one
        var patient1 = await SeedPatientAsync("keep@clinic.com");
        var patient2 = await SeedPatientAsync("delete@clinic.com");

        await _patientService.DeleteAsync(patient2.Id);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _patientService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(patient1.Id, result.Value.Items.First().Id);
    }
}
