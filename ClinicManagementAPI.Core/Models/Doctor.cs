using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Core.Models;

public class Doctor : ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ISoftDeletable implementation
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
