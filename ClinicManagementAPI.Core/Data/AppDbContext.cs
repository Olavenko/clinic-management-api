using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must call base — Identity needs it to configure its own tables
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            // Each RefreshToken belongs to one User
            entity.HasOne(rt => rt.User)
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique index on Token for fast lookups
            entity.HasIndex(rt => rt.Token)
                  .IsUnique();
        });

        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            // Filtered unique index: only active (non-deleted) emails must be unique
            entity.HasIndex(p => p.Email)
                  .IsUnique()
                  .HasFilter("IsDeleted = 0");

            // Optional relationship to ApplicationUser
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .IsRequired(false);

            // Global query filter: automatically exclude soft-deleted patients
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // Doctor configuration
        modelBuilder.Entity<Doctor>(entity =>
        {
            // Filtered unique index: only active (non-deleted) emails must be unique
            entity.HasIndex(d => d.Email)
                  .IsUnique()
                  .HasFilter("IsDeleted = 0");

            // Global query filter: automatically exclude soft-deleted doctors
            entity.HasQueryFilter(d => !d.IsDeleted);
        });
    }
}
