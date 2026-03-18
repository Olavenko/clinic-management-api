# ADR 0002: Post-Sprint-Codebase-Review-Remediation

- **Status**: Accepted
- **Date**: 2026-03-18
- **Owners**: Mohamed Ali
- **Related**: Post-Sprint 6 — Full Codebase Review
- **Tags**: bugs, security, correctness, consistency, data-integrity, validation

---

## Context

After completing all 6 sprints of the Clinic Management API, a comprehensive codebase review was performed using Claude Code's scan feature. The review identified 22 issues across 6 categories: Bugs (4), Security (5), Correctness (3), Consistency (5), and Minor/Nitpicks (5). This ADR documents all fixes applied, decisions to defer, and issues reviewed but kept as-is.

## Issues Addressed

### Fixed

| #   | Severity | Category    | Summary                                                        | Location                          |
| --- | -------- | ----------- | -------------------------------------------------------------- | --------------------------------- |
| B1  | Critical | Bug         | CleanupExpiredTokensAsync never called SaveChangesAsync        | AuthService.cs:220-232            |
| B2  | High     | Bug         | Same-day past-time appointments allowed                        | AppointmentService.cs:164         |
| B3  | High     | Bug         | TimeOnly.AddMinutes wraps past midnight breaking overlap check | AppointmentService.cs:176,294     |
| B4  | Medium   | Bug         | NullReferenceException when Doctor is soft-deleted              | AppointmentService.cs:268         |
| S1  | Critical | Security    | Missing ValidationFilter on AssignRole endpoint                | UserEndpoints.cs:15-27            |
| S3  | Medium   | Security    | No MaxLength on RefreshTokenRequest.RefreshToken               | RefreshTokenRequest.cs:8          |
| S4  | Medium   | Security    | JWT Key length not validated at startup                        | Program.cs:58-76                  |
| C1  | High     | Correctness | UpdateStatusAsync lacks Serializable transaction               | AppointmentService.cs:355-384     |
| C3  | Medium   | Correctness | Failed entity stuck in Change Tracker after DbUpdateException  | PatientService.cs, DoctorService.cs |
| K1  | —        | Consistency | IsNullOrEmpty vs IsNullOrWhiteSpace inconsistency              | PatientService.cs:19              |
| K2  | —        | Consistency | MaxLength on Bio missing in CreateDoctorRequest                | CreateDoctorRequest.cs:28         |
| K4  | —        | Consistency | Models lack MaxLength — DB columns default to nvarchar(max)    | Patient.cs, Doctor.cs, ApplicationUser.cs, RefreshToken.cs |
| K5  | —        | Consistency | Missing index on RefreshToken.UserId                           | AppDbContext.cs:20-30             |
| N2  | —        | Minor       | LoginRequest.Password has no MinLength                         | LoginRequest.cs:12                |
| N3  | —        | Minor       | No MaxLength on email fields across DTOs                       | Multiple DTOs                     |
| N4  | —        | Minor       | Appointment.Notes model property has no MaxLength              | Appointment.cs:21                 |

### Deferred / Known Limitations

| #   | Severity | Category    | Summary                                                    | Reason                                                                                       |
| --- | -------- | ----------- | ---------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| S2  | High     | Security    | No integration test for AssignRole authorization           | Test gap — requires new integration tests (planned)                                          |
| S5  | Low      | Security    | Validation runs before Authorization leaking request info  | Low risk; proper fix requires middleware reordering that could affect all endpoints           |
| C2  | Medium   | Correctness | GenerateJwtToken doesn't accept CancellationToken          | Not fixable — UserManager.GetRolesAsync() does not accept CancellationToken (Identity API limitation) |
| K3  | —        | Consistency | Validation error messages inconsistent across DTOs         | Cosmetic; high effort for low value in a portfolio project                                   |

### Reviewed — No Change Needed

| #   | Severity | Category | Summary                                          | Reason                                                              |
| --- | -------- | -------- | ------------------------------------------------ | ------------------------------------------------------------------- |
| N1  | —        | Minor    | YearsOfExperience allows 0                       | Valid for fresh graduates/interns — design decision to keep as-is   |
| N5  | —        | Minor    | Route ordering in AppointmentEndpoints            | Routes have distinct patterns (literal vs parameter) — no ambiguity |

## Decision

### Bugs

- **B1**: Added `await dbContext.SaveChangesAsync(cancellationToken)` inside `CleanupExpiredTokensAsync` after `RemoveRange` — making the method self-contained instead of relying on the caller to persist changes.
- **B2**: Added same-day time validation in both `CreateAsync` and `UpdateAsync` — if `AppointmentDate == today`, then `AppointmentTime` must be in the future.
- **B3**: Added midnight boundary validation before the transaction in both `CreateAsync` and `UpdateAsync` — calculates `endMinutes` using integer arithmetic instead of `TimeOnly.AddMinutes()` to avoid wrap-around. Rejects appointments that would cross midnight.
- **B4**: Added null check for `appointment.Doctor` before accessing `IsAvailable` in `UpdateAsync` — returns 404 "Doctor no longer exists" if the doctor was soft-deleted.

### Security

- **S1**: Added `.AddEndpointFilter<ValidationFilter<AssignRoleRequest>>()` to the AssignRole endpoint chain.
- **S3**: Added `[MaxLength(256)]` to `RefreshTokenRequest.RefreshToken` — prevents oversized payloads. Value based on Base64 output of 64 bytes (88 chars) with safe margin.
- **S4**: Added JWT Key length validation at startup — throws `InvalidOperationException` if key is missing or less than 32 characters (minimum for HMAC-SHA256).

### Correctness

- **C1**: Wrapped `UpdateStatusAsync` read + write in a Serializable transaction with `DbUpdateException` catch — prevents concurrent status transitions on the same appointment.
- **C3**: Added `context.Entry(entity).State = EntityState.Detached` in catch blocks of `CreateAsync` in both `PatientService` and `DoctorService` — prevents orphaned entities in the Change Tracker.

### Consistency & Data Integrity

- **K1**: Changed `IsNullOrEmpty` to `IsNullOrWhiteSpace` in `PatientService.GetAllAsync`.
- **K2**: Added `[MaxLength(500)]` to `CreateDoctorRequest.Bio` to match `UpdateDoctorRequest`.
- **K4**: Added `[MaxLength]` attributes to all Model entities (Patient, Doctor, ApplicationUser, RefreshToken) to match DTO constraints. Generated EF migration `AddMaxLengthConstraints` to update database columns from `nvarchar(max)` to specific sizes. Additionally ran a full DTO/Model alignment scan to ensure all annotations are consistent across the codebase.
- **K5**: Added `entity.HasIndex(rt => rt.UserId)` to `RefreshToken` configuration in `AppDbContext` — supports `RevokeAllUserTokensAsync` and `CleanupExpiredTokensAsync` queries.

### Minor

- **N2**: Added `[MinLength(8)]` to `LoginRequest.Password` to match `RegisterRequest` — prevents pointless authentication attempts with impossible passwords.
- **N3**: Added `[MaxLength(255)]` to all Email properties across DTOs (RegisterRequest, LoginRequest, CreatePatientRequest, UpdatePatientRequest, CreateDoctorRequest, UpdateDoctorRequest).
- **N4**: Added `[MaxLength(500)]` to `Appointment.Notes` model property (done via Agent alignment scan).

## Alternatives Considered

| Option                                      | Pros                                    | Cons                                          | Verdict  |
| ------------------------------------------- | --------------------------------------- | --------------------------------------------- | -------- |
| B3: Allow cross-midnight appointments       | More flexible scheduling                | Breaks TimeOnly arithmetic and overlap logic   | Rejected |
| B3: Convert TimeOnly to TimeSpan for math   | Avoids wrap-around                      | Requires changes across entire codebase        | Rejected |
| B3: Validate endMinutes with integer math   | Simple, no codebase changes needed      | Clinics cannot schedule past midnight          | Adopted  |
| C1: Optimistic concurrency with RowVersion  | Less locking overhead                   | Requires schema change and migration           | Rejected |
| C1: Serializable transaction                | Consistent with CreateAsync/UpdateAsync | Slightly more DB locking                       | Adopted  |
| C2: Pass CancellationToken to GenerateJwt   | Proper cancellation propagation         | Identity API doesn't support it                | Not Applicable |
| S5: Reorder middleware pipeline             | Proper auth-before-validation           | High risk of breaking all endpoints            | Deferred |

## Testing

- Manual verification: all endpoints tested via HTTP requests after each fix
- Build verification: `dotnet build` passes with zero errors and zero warnings
- Database migrations: `AddMaxLengthConstraints`, `AlignAnnotationsConsistency`, and `AddRefreshTokenUserIdIndex` applied successfully
- **Planned**: Integration tests for authorization (S2), pagination boundaries, token rotation flow, and edge cases identified in the Test Coverage Gaps section of the review

## Consequences

- **Benefits**: 4 bugs eliminated, 3 security gaps closed, data integrity enforced at both API and database layers, consistent validation across all DTOs and Models, improved query performance with UserId index on RefreshTokens
- **Drawbacks**: 3 additional EF migrations added to the migration history; appointments cannot cross midnight (acceptable constraint for a clinic system)
- **Trade-offs**: Some issues (S5, K3) deferred to avoid high-risk changes with low return; C2 not fixable due to framework limitation
- **Technical Debt Remaining**: S2 (authorization tests), S5 (validation/auth ordering), K3 (error message consistency)

## References

- Codebase review output from Claude Code scan (March 18, 2026)
- [ADR-Template.md](../ADR-Template.md)
- [Index-ADR.md](../Index-ADR.md)
- EF Migrations: `AddMaxLengthConstraints`, `AlignAnnotationsConsistency`, `AddRefreshTokenUserIdIndex`
