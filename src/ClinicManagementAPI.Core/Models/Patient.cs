using System.ComponentModel.DataAnnotations;

using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Core.Models;

// Patient entity — supports soft delete via ISoftDeletable
// UserId is optional: null when receptionist adds patient, set when patient self-registers
public class Patient : ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Optional FK to ApplicationUser — null if added by receptionist
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // ISoftDeletable implementation
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
