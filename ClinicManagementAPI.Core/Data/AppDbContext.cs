using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Must call base — Identity needs it to configure its own tables
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            // Restrict delete: deleting a user must not cascade-delete token history
            entity.HasOne(rt => rt.User)
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(rt => rt.Token)
                  .IsUnique();
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            // HasFilter: allow email reuse after soft delete (IsDeleted = 1 emails are excluded)
            entity.HasIndex(p => p.Email)
                  .IsUnique()
                  .HasFilter("IsDeleted = 0");

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .IsRequired(false);

            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            // HasFilter: allow email reuse after soft delete
            entity.HasIndex(d => d.Email)
                  .IsUnique()
                  .HasFilter("IsDeleted = 0");

            entity.HasQueryFilter(d => !d.IsDeleted);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasOne(a => a.Patient)
                  .WithMany()
                  .HasForeignKey(a => a.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                  .WithMany()
                  .HasForeignKey(a => a.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Match the query filters on Doctor and Patient
            // so EF Core doesn't return appointments for deleted entities
            entity.HasQueryFilter(a => !a.Patient.IsDeleted && !a.Doctor.IsDeleted);
        });
    }
}
