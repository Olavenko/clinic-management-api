using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.Models;
using ClinicManagementAPI.Core.Services;

using Microsoft.EntityFrameworkCore;

namespace ClinicManagementAPI.Tests.Unit;

public class DoctorServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly DoctorService _doctorService;

    public DoctorServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _doctorService = new DoctorService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Helper Methods ──────────────────────────────────────────────

    private static CreateDoctorRequest CreateValidRequest(string? email = null) => new()
    {
        FullName = "Dr. Ahmed Hassan",
        Email = email ?? $"dr_{Guid.NewGuid()}@clinic.com",
        Phone = "+201234567890",
        Specialization = "Cardiology",
        YearsOfExperience = 10,
        Bio = "Experienced cardiologist"
    };

    private async Task<DoctorResponse> SeedDoctorAsync(
        string? email = null, bool isAvailable = true)
    {
        var request = CreateValidRequest(email);
        var result = await _doctorService.CreateAsync(request);

        if (!isAvailable)
        {
            var updateRequest = new UpdateDoctorRequest { IsAvailable = false };
            var updated = await _doctorService.UpdateAsync(result.Value!.Id, updateRequest);
            return updated.Value!;
        }

        return result.Value!;
    }

    private async Task SeedMultipleDoctorsAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _doctorService.CreateAsync(new CreateDoctorRequest
            {
                FullName = $"Doctor {i:D2}",
                Email = $"doctor{i}@clinic.com",
                Phone = $"+2012345678{i:D2}",
                Specialization = i % 2 == 0 ? "Cardiology" : "Dentistry",
                YearsOfExperience = i + 1,
                Bio = $"Bio for doctor {i}"
            });
        }
    }

    // ── GetAll + Search ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedDoctors()
    {
        // Arrange
        await SeedMultipleDoctorsAsync(5);
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithPage2_ReturnsCorrectDoctors()
    {
        // Arrange — seed 15 doctors, request page 2 with pageSize 10
        await SeedMultipleDoctorsAsync(15);
        var pagination = new PaginationRequest { Page = 2, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Items.Count()); // 15 - 10 = 5 on page 2
        Assert.Equal(2, result.Value.Page);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_FiltersByNameOrSpecialization()
    {
        // Arrange — seed doctors with distinct specializations
        await _doctorService.CreateAsync(new CreateDoctorRequest
        {
            FullName = "Dr. Ahmed",
            Email = "ahmed@clinic.com",
            Phone = "+201111111111",
            Specialization = "Cardiology",
            YearsOfExperience = 10
        });
        await _doctorService.CreateAsync(new CreateDoctorRequest
        {
            FullName = "Dr. Sara",
            Email = "sara@clinic.com",
            Phone = "+201222222222",
            Specialization = "Dentistry",
            YearsOfExperience = 5
        });

        var pagination = new PaginationRequest { Page = 1, PageSize = 10, SearchTerm = "Cardiology" };

        // Act
        var result = await _doctorService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Dr. Ahmed", result.Value.Items.First().FullName);
    }

    [Fact]
    public async Task GetAllAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange — seed doctors but search for nonexistent term
        await SeedDoctorAsync();
        var pagination = new PaginationRequest { Page = 1, PageSize = 10, SearchTerm = "NonExistent" };

        // Act
        var result = await _doctorService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotReturnDeletedDoctors()
    {
        // Arrange — create and then soft-delete a doctor
        var doctor = await SeedDoctorAsync();
        await _doctorService.DeleteAsync(doctor.Id);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    // ── GetAvailable ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableAsync_ReturnsOnlyAvailableDoctors()
    {
        // Arrange — create one available and one unavailable doctor
        await SeedDoctorAsync("available@clinic.com", isAvailable: true);
        await SeedDoctorAsync("unavailable@clinic.com", isAvailable: false);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAvailableAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.True(result.Value.Items.First().IsAvailable);
    }

    [Fact]
    public async Task GetAvailableAsync_DoesNotReturnDeletedDoctors()
    {
        // Arrange — create and soft-delete an available doctor
        var doctor = await SeedDoctorAsync();
        await _doctorService.DeleteAsync(doctor.Id);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAvailableAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAvailableAsync_WithSearchTerm_FiltersCorrectly()
    {
        // Arrange — create two available doctors with different specializations
        await _doctorService.CreateAsync(new CreateDoctorRequest
        {
            FullName = "Dr. Heart",
            Email = "heart@clinic.com",
            Phone = "+201111111111",
            Specialization = "Cardiology",
            YearsOfExperience = 10
        });
        await _doctorService.CreateAsync(new CreateDoctorRequest
        {
            FullName = "Dr. Teeth",
            Email = "teeth@clinic.com",
            Phone = "+201222222222",
            Specialization = "Dentistry",
            YearsOfExperience = 5
        });

        var pagination = new PaginationRequest { Page = 1, PageSize = 10, SearchTerm = "Dentistry" };

        // Act
        var result = await _doctorService.GetAvailableAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Dr. Teeth", result.Value.Items.First().FullName);
    }

    // ── GetById ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSuccessResult()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();

        // Act
        var result = await _doctorService.GetByIdAsync(doctor.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(doctor.Id, result.Value!.Id);
        Assert.Equal(doctor.FullName, result.Value.FullName);
        Assert.Equal(doctor.Email, result.Value.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Act
        var result = await _doctorService.GetByIdAsync(9999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Doctor not found", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedDoctor_ReturnsFailureResult()
    {
        // Arrange — create and soft-delete a doctor
        var doctor = await SeedDoctorAsync();
        await _doctorService.DeleteAsync(doctor.Id);

        // Act
        var result = await _doctorService.GetByIdAsync(doctor.Id);

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
        var result = await _doctorService.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.FullName, result.Value.FullName);
        Assert.Equal(request.Email, result.Value.Email);
        Assert.Equal(request.Phone, result.Value.Phone);
        Assert.Equal(request.Specialization, result.Value.Specialization);
        Assert.Equal(request.YearsOfExperience, result.Value.YearsOfExperience);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsFailureResult()
    {
        // Arrange — create a doctor first
        var email = "duplicate@clinic.com";
        await SeedDoctorAsync(email);

        var duplicateRequest = CreateValidRequest(email);

        // Act
        var result = await _doctorService.CreateAsync(duplicateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithDeletedDoctorEmail_ReturnsFailureResult()
    {
        // Arrange — create and soft-delete a doctor
        var email = "deleted@clinic.com";
        var doctor = await SeedDoctorAsync(email);
        await _doctorService.DeleteAsync(doctor.Id);

        // Act — try to create with same email (should fail: IgnoreQueryFilters check)
        var request = CreateValidRequest(email);
        var result = await _doctorService.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    [Fact]
    public async Task CreateAsync_SetsIsAvailableToTrue_ByDefault()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _doctorService.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsAvailable);
    }

    // ── Update ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WithValidId_ReturnsSuccessResult()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();
        var updateRequest = new UpdateDoctorRequest { FullName = "Updated Name" };

        // Act
        var result = await _doctorService.UpdateAsync(doctor.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value!.FullName);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Arrange
        var updateRequest = new UpdateDoctorRequest { FullName = "Updated" };

        // Act
        var result = await _doctorService.UpdateAsync(9999, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Doctor not found", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithAllFieldsNull_ReturnsFailureResult()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();
        var updateRequest = new UpdateDoctorRequest(); // all fields null

        // Act
        var result = await _doctorService.UpdateAsync(doctor.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("At least one field must be provided", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ReturnsFailureResult()
    {
        // Arrange — create two doctors
        var doctor1 = await SeedDoctorAsync("doctor1@clinic.com");
        var doctor2 = await SeedDoctorAsync("doctor2@clinic.com");

        // Try to update doctor2's email to doctor1's email
        var updateRequest = new UpdateDoctorRequest { Email = "doctor1@clinic.com" };

        // Act
        var result = await _doctorService.UpdateAsync(doctor2.Id, updateRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithNewUniqueEmail_ReturnsSuccessResult()
    {
        // Arrange
        var doctor = await SeedDoctorAsync("original@clinic.com");
        var updateRequest = new UpdateDoctorRequest { Email = "newemail@clinic.com" };

        // Act
        var result = await _doctorService.UpdateAsync(doctor.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("newemail@clinic.com", result.Value!.Email);
    }

    [Fact]
    public async Task UpdateAsync_CanSetIsAvailableToFalse()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();
        var updateRequest = new UpdateDoctorRequest { IsAvailable = false };

        // Act
        var result = await _doctorService.UpdateAsync(doctor.Id, updateRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsAvailable);
    }

    // ── Delete (Soft Delete) ────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithValidId_SetsIsDeletedTrue()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();

        // Act
        var result = await _doctorService.DeleteAsync(doctor.Id);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify in DB (bypass query filter)
        var dbDoctor = await _dbContext.Doctors
            .IgnoreQueryFilters()
            .FirstAsync(d => d.Id == doctor.Id);
        Assert.True(dbDoctor.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_SetsDeletedAtToUtcNow()
    {
        // Arrange
        var doctor = await SeedDoctorAsync();
        var beforeDelete = DateTime.UtcNow;

        // Act
        var result = await _doctorService.DeleteAsync(doctor.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var dbDoctor = await _dbContext.Doctors
            .IgnoreQueryFilters()
            .FirstAsync(d => d.Id == doctor.Id);
        Assert.NotNull(dbDoctor.DeletedAt);
        Assert.True(dbDoctor.DeletedAt >= beforeDelete);
        Assert.True(dbDoctor.DeletedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFailureResult()
    {
        // Act
        var result = await _doctorService.DeleteAsync(9999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Doctor not found", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_DoctorDisappearsFromGetAll()
    {
        // Arrange — create two doctors, delete one
        var doctor1 = await SeedDoctorAsync("keep@clinic.com");
        var doctor2 = await SeedDoctorAsync("delete@clinic.com");

        await _doctorService.DeleteAsync(doctor2.Id);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAllAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(doctor1.Id, result.Value.Items.First().Id);
    }

    [Fact]
    public async Task DeleteAsync_DoctorDisappearsFromGetAvailable()
    {
        // Arrange — create two available doctors, delete one
        var doctor1 = await SeedDoctorAsync("keep@clinic.com");
        var doctor2 = await SeedDoctorAsync("delete@clinic.com");

        await _doctorService.DeleteAsync(doctor2.Id);

        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };

        // Act
        var result = await _doctorService.GetAvailableAsync(pagination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(doctor1.Id, result.Value.Items.First().Id);
    }
}
