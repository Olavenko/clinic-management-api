namespace ClinicManagementAPI.Core.Interfaces;

// Contract for any entity that supports soft delete
// Instead of permanently removing records, we mark them as deleted
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
