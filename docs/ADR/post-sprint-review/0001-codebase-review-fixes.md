# ADR 0001: Codebase-Review-Fixes

- **Status**: Accepted
- **Date**: 2026-03-17
- **Owners**: Mohamed Ali
- **Related**: Post Sprint 6 — Code Review
- **Tags**: bugs, architecture, consistency, security, validation, concurrency

---

## Context

After completing all 6 sprints of the Clinic Management API, a comprehensive codebase review was conducted. The review identified 13 issues across 4 severity categories: bugs/correctness, design/architecture, consistency, and minor fixes. All 13 issues were addressed in a single pass, with each fix understood, implemented manually, and verified before moving to the next.

## Issues Addressed

| #  | Severity | Category     | Summary                                                    | Location                                       |
|----|----------|--------------|------------------------------------------------------------|-------------------------------------------------|
| 1  | Critical | Bug          | Missing ValidationFilter on CRUD write endpoints           | PatientEndpoints, DoctorEndpoints, AppointmentEndpoints |
| 2  | Critical | Bug          | Race condition on email uniqueness (check-then-act)        | PatientService.CreateAsync/UpdateAsync, DoctorService.CreateAsync/UpdateAsync |
| 3  | Critical | Bug          | Race condition on appointment overlap detection            | AppointmentService.CreateAsync/UpdateAsync      |
| 4  | High     | Architecture | No XxxMappings.cs extension methods (documented but missing) | DoctorService (5x duplication), PatientService, AppointmentService |
| 5  | High     | Architecture | Pagination clamping duplicated 9 times across services     | PatientService, DoctorService, AppointmentService |
| 6  | Medium   | Architecture | ExpiresAt in AuthService response is inaccurate            | AuthService.RegisterAsync, LoginAsync, RefreshTokenAsync |
| 7  | Medium   | Consistency  | AuthEndpoints uses inline lambdas; others use static methods | AuthEndpoints.cs                               |
| 8  | Medium   | Consistency  | Search case sensitivity differs between SQL Server and InMemory | PatientService, DoctorService, AppointmentService |
| 9  | Low      | Consistency  | PatientEndpoints has redundant group-level RequireAuthorization | PatientEndpoints.cs:13                         |
| 10 | Low      | Minor        | No comment explaining why "Doctor" excluded from AssignRoleAsync | AuthService.AssignRoleAsync                    |
| 11 | Low      | Minor        | user! null-forgiving operator in RefreshTokenAsync         | AuthService.RefreshTokenAsync:135              |
| 12 | Low      | Minor        | UpdateAppointment doesn't validate doctor availability     | AppointmentService.UpdateAsync                 |
| 13 | Low      | Minor        | No CORS configuration in Program.cs                        | Program.cs                                     |

## Decision

All 13 issues were fixed. Below is what was done for each fix.

### Fix 1 — Add ValidationFilter to CRUD Endpoints

Added `.AddEndpointFilter<ValidationFilter<T>>()` to all write endpoints (Create/Update) across Patient, Doctor, and Appointment endpoints. This ensures Data Annotations (`[Required]`, `[EmailAddress]`, `[Range]`, etc.) are enforced at the endpoint level before reaching the service. 7 endpoints updated in total.

**Files changed:** `PatientEndpoints.cs`, `DoctorEndpoints.cs`, `AppointmentEndpoints.cs`

### Fix 2 — Handle DbUpdateException for Email Race Condition

Wrapped `SaveChangesAsync` in try/catch for `DbUpdateException` in all 4 methods that perform email uniqueness checks (PatientService Create/Update, DoctorService Create/Update). The DB filtered unique index catches concurrent duplicates that pass the application-level check, and now returns a clean `Result.Failure` (400) instead of an unhandled 500 error.

**Files changed:** `PatientService.cs`, `DoctorService.cs`

### Fix 3 — Protect Appointment Overlap from Race Condition

Wrapped the overlap check + save in a Serializable transaction in both `CreateAsync` and `UpdateAsync`. This prevents two concurrent requests from booking the same time slot. Unlike email uniqueness, there is no DB-level constraint for time overlaps, so the transaction is the only protection.

Key design decisions:
- Validation (patient/doctor exists, date not in past, status check) stays **outside** the transaction — no concurrency risk there
- Overlap check + apply updates + SaveChanges + Commit are all **inside** the try block — atomic operation
- `DbUpdateException` catch returns 409 Conflict (not 400) — more accurate HTTP semantics
- `RollbackAsync` called before every early return inside the transaction

**Files changed:** `AppointmentService.cs`

### Fix 4 — Create XxxMappings.cs Extension Methods

Created 3 mapping files in `Core/Models/` as documented in DEVELOPMENT-STANDARDS.md:
- `PatientMappings.cs` — `patient.ToResponse()` extension method
- `DoctorMappings.cs` — `doctor.ToResponse()` extension method
- `AppointmentMappings.cs` — `appointment.ToResponse()` extension method

Removed all `private static MapToResponse` methods and inline `new XxxResponse { ... }` blocks from services. DoctorService had the worst duplication (same 10-line mapping repeated 5 times).

For DoctorService `GetAllAsync` and `GetAvailableAsync`, changed from `.Select(d => d.ToResponse()).ToListAsync()` (which causes EF Core client-side evaluation warnings) to `.ToListAsync()` first, then `.Select(d => d.ToResponse()).ToList()` in memory.

**Files created:** `PatientMappings.cs`, `DoctorMappings.cs`, `AppointmentMappings.cs`
**Files changed:** `PatientService.cs`, `DoctorService.cs`, `AppointmentService.cs`

### Fix 5 — Centralize Pagination Clamping

Moved the default-clamping logic from 9 service methods into `PaginationRequest` property setters:

```csharp
public int Page
{
    get => _page;
    set => _page = value < 1 ? 1 : value;
}

public int PageSize
{
    get => _pageSize;
    set => _pageSize = value < 1 ? 10 : value > 50 ? 50 : value;
}
```

Added upper bound clamping on PageSize (max 50) to match the existing `[Range(1, 50)]` Data Annotation. Since `AppointmentFilterRequest` inherits from `PaginationRequest`, the clamping applies automatically to appointment queries too.

**Files changed:** `PaginationRequest.cs`, `PatientService.cs`, `DoctorService.cs`, `AppointmentService.cs`

### Fix 6 — Fix Inaccurate ExpiresAt in AuthService

Captured `DateTime.UtcNow` in a single `utcNow` variable and reused it across token generation, refresh token creation, and response ExpiresAt. Previously each used its own `DateTime.UtcNow` call, creating slight timing mismatches.

Updated `GenerateJwtToken` to accept a `DateTime utcNow` parameter instead of calling `DateTime.UtcNow` internally. Applied to all 3 methods: `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`.

**Files changed:** `AuthService.cs`

### Fix 7 — Refactor AuthEndpoints to Private Static Methods

Converted all 4 Auth endpoint handlers from inline lambdas to private static methods (`Register`, `Login`, `Refresh`, `Logout`), matching the pattern used in Patient, Doctor, and Appointment endpoints.

**Files changed:** `AuthEndpoints.cs`

### Fix 8 — Fix Search Case Sensitivity Mismatch

Added `.ToLower()` on both the search term and entity properties in all search queries. This ensures case-insensitive search on both SQL Server (which was already case-insensitive via collation) and InMemory provider (used in tests, which is case-sensitive by default).

The `searchTerm.ToLower()` is computed once in a variable before the query to avoid redundant computation.

Applied to 4 search locations: PatientService `GetAllAsync`, DoctorService `GetAllAsync` and `GetAvailableAsync`, AppointmentService `GetAllAsync`.

**Files changed:** `PatientService.cs`, `DoctorService.cs`, `AppointmentService.cs`

### Fix 9 — Remove Redundant RequireAuthorization

Removed `.RequireAuthorization()` from the PatientEndpoints group level. Every endpoint already specifies its own `.RequireAuthorization(policy => ...)` with specific role policies. No other endpoint file had group-level auth.

**Files changed:** `PatientEndpoints.cs`

### Fix 10 — Add Comment Explaining Doctor Role Exclusion

Added a comment above the `validRoles` array in `AssignRoleAsync` explaining that the Doctor role is seeded but intentionally excluded from assignment — doctors are managed through the Doctors CRUD endpoints, not through user roles.

**Files changed:** `AuthService.cs`

### Fix 11 — Replace user! Null-Forgiving with Proper Null Check

Replaced `user!` in `RefreshTokenAsync` with a proper null check that returns `Result.Failure("User not found", 404)`. This prevents a `NullReferenceException` if a user is deleted while their refresh tokens still exist (due to `DeleteBehavior.Restrict`).

**Files changed:** `AuthService.cs`

### Fix 12 — Validate Doctor Availability in UpdateAppointment

Added `IsAvailable` check on the doctor in `UpdateAsync`, matching the same check that already exists in `CreateAsync`. Prevents rescheduling an appointment to a doctor who has been marked unavailable after the original booking.

**Files changed:** `AppointmentService.cs`

### Fix 13 — Add CORS Configuration

Added inline CORS policy in the middleware pipeline (`Program.cs`) using `AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`. Positioned after `UseHttpsRedirection` and before `UseAuthentication` (order matters for the auth pipeline).

Includes a comment noting this is suitable for development/portfolio and should be restricted in production.

**Files changed:** `Program.cs`

## Alternatives Considered

| Fix | Option                           | Pros                    | Cons                           | Verdict  |
|-----|----------------------------------|-------------------------|--------------------------------|----------|
| 3   | Pessimistic locking (row locks)  | Fine-grained control    | Requires raw SQL, complex      | Rejected |
| 3   | Serializable transaction         | Simple, EF Core native  | Slightly higher lock contention | Adopted  |
| 5   | Endpoint filter for pagination   | Separates concerns      | Adds a new file, more complex  | Rejected |
| 5   | Property setters in PaginationRequest | Single place, simple | Mixes validation with DTO      | Adopted  |
| 8   | StringComparison.OrdinalIgnoreCase | C# native             | Doesn't translate to SQL       | Rejected |
| 8   | ToLower() on both sides          | Works in SQL and memory | Minor SQL overhead (negligible) | Adopted  |

## Testing

- All 181 existing tests should continue passing after these changes
- Fix 8 (case sensitivity) may cause previously passing tests to behave differently — tests that relied on exact case matching now work case-insensitively, matching production behavior
- Fix 3 (serializable transactions) cannot be tested with InMemory provider — InMemory does not support transactions. Tests verify business logic; concurrency protection is verified against SQL Server
- Manual verification: `dotnet test --verbosity normal` after all changes

## Consequences

### Benefits
- **Security**: Invalid data no longer reaches services (Fix 1). Race conditions on email and appointments are handled (Fixes 2, 3)
- **Correctness**: ExpiresAt accurately reflects token expiry (Fix 6). Doctor availability validated on reschedule (Fix 12). Null reference prevented (Fix 11)
- **Maintainability**: Mapping logic centralized in 3 files instead of scattered across services (Fix 4). Pagination clamping in one place (Fix 5)
- **Consistency**: All endpoints follow the same code style (Fixes 7, 9). Search behavior matches between test and production (Fix 8)
- **Frontend-ready**: CORS enabled for browser-based clients (Fix 13)

### Trade-offs
- Serializable transactions (Fix 3) have higher lock contention than optimistic approaches — acceptable for a clinic management system's expected load
- `ToLower()` in SQL queries (Fix 8) adds a minor overhead on SQL Server where the default collation is already case-insensitive — negligible performance impact

## References

- `docs/DEVELOPMENT-STANDARDS.md` — Section 3.2 (Mapping Convention), Section 3.4 (Endpoint Organization)
- `CLAUDE.md` — Architecture Decisions (Validation, Mapping, Soft Delete)
- `ClinicManagementAPI.Api/Filters/ValidationFilter.cs` — Existing filter implementation
- `ClinicManagementAPI.Core/Data/AppDbContext.cs` — Filtered unique indexes, query filters
