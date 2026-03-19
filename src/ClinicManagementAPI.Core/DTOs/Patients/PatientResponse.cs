namespace ClinicManagementAPI.Core.DTOs.Patients;

// No IsDeleted or DeletedAt — these are internal fields not exposed to clients
public class PatientResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
