using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Core.Models;

public class Doctor : ISoftDeletable
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    public string? Bio { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ISoftDeletable implementation
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
