# Sprint 3 — Patients CRUD + Role-based Authorization + Assign Role

**Project:** Clinic Management API  
**Sprint Duration:** 1 Week  
**Expected Time:** ~6.5 hours  
**Goal:** Full Patients CRUD with Role-based Authorization + Admin can assign roles to users  
**Prerequisites:** Sprint 2 is complete, JWT + Identity + Result Pattern are working, CI is green

---

## Section 1 — Soft Delete Interface

**Expected Time: 10 minutes**  
**Goal:** Define a reusable interface for Soft Delete — used by Patient now and Doctor in Sprint 4

```markdown
[x] Create ISoftDeletable interface in Core/Interfaces/
    Command : New-Item -Path "ClinicManagementAPI.Core/Interfaces/ISoftDeletable.cs" -ItemType File -Force
    Properties:
    - IsDeleted (bool)
    - DeletedAt (DateTime, nullable)

[x] This interface will be used by:
    - Patient model (this sprint)
    - Doctor model (Sprint 4)
    - Any future entity that needs soft delete
```

**Why an interface?**

```markdown
Copy-paste IsDeleted in every model → Easy to forget one → Inconsistent ❌
ISoftDeletable interface             → Enforced contract + enables Global Query Filter ✅
```

---

## Section 2 — Patient Model

**Expected Time: 30 minutes**  
**Goal:** Define the Patient entity with an optional link to ApplicationUser and Soft Delete support

```markdown
[x] Create Gender enum in Core/Models/
    Command : New-Item -Path "ClinicManagementAPI.Core/Models/Gender.cs" -ItemType File -Force
    public enum Gender { Male, Female }

[x] Create Patient model in Core/Models/
    Command : New-Item -Path "ClinicManagementAPI.Core/Models/Patient.cs" -ItemType File -Force
    Implements: ISoftDeletable
    Properties:
    - Id (int)
    - FullName (string, required)
    - Email (string, required)
    - Phone (string, required)
    - DateOfBirth (DateOnly)
    - Gender (Gender enum)
    - Address (string, optional)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime, nullable)
    - UserId (string, nullable → optional FK to ApplicationUser)
    - User (ApplicationUser, nullable → navigation property)
    - IsDeleted (bool, default = false) ← from ISoftDeletable
    - DeletedAt (DateTime, nullable)    ← from ISoftDeletable

    ⚠️ Why UserId is optional:
    - Patient registered themselves → UserId links to their account
    - Receptionist added the patient → UserId is null (no account yet)

[x] Configure Patient entity in AppDbContext OnModelCreating
    - Filtered Unique Index on Email: .HasIndex(p => p.Email).IsUnique().HasFilter("IsDeleted = 0")
    - UserId has optional relationship: .HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).IsRequired(false)
    - Global Query Filter: .HasQueryFilter(p => !p.IsDeleted)

    ⚠️ Why Filtered Index (not plain Unique)?
    - Plain Unique → Deleted patient blocks new patient with same email → Bad UX ❌
    - Filtered Unique → Only active (IsDeleted = 0) emails must be unique → Correct ✅
    - Allows: active "ahmed@mail.com" + deleted "ahmed@mail.com" in same table

    ⚠️ Global Query Filter means:
    - Every query automatically adds WHERE IsDeleted = false
    - Deleted patients are invisible to all endpoints
    - No need to add IsDeleted check in every service method
    - Can bypass with .IgnoreQueryFilters() when needed

[x] Add Patients DbSet to AppDbContext
    public DbSet<Patient> Patients => Set<Patient>();

[x] Add Patient Migration
    Command: dotnet ef migrations add AddPatients --project ClinicManagementAPI.Core
                                                  --startup-project ClinicManagementAPI.Api

[x] Apply Migration
    Command: dotnet ef database update --project ClinicManagementAPI.Core
                                       --startup-project ClinicManagementAPI.Api
```

**Why Soft Delete?**

```markdown
Hard Delete → Patient record gone forever → Appointment history breaks ❌
Soft Delete → Patient hidden but data preserved → FK relationships safe ✅
Medical data → Should never be permanently deleted → Compliance ✅
```

**Why Global Query Filter?**

```markdown
Manual filter in every query → Easy to forget one → Deleted data leaks ❌
Global Query Filter          → EF Core handles it automatically → Safe ✅
```

---

## Section 3 — DTOs + Validation

**Expected Time: 20 minutes**  
**Goal:** Define request and response shapes — never expose the raw model to the API consumer

```markdown
[x] Create CreatePatientRequest DTO in Api/DTOs/Patients/
    Command : New-Item -Path "ClinicManagementAPI.Api/DTOs/Patients/CreatePatientRequest.cs" -ItemType File -Force
    Properties:
    - FullName (string, [Required], [MinLength(2)], [MaxLength(100)])
    - Email (string, [Required], [EmailAddress])
    - Phone (string, [Required], [Phone])
    - DateOfBirth (DateOnly, [Required])
    - Gender (Gender enum, [Required])
    - Address (string, optional, [MaxLength(250)])

[x] Create UpdatePatientRequest DTO in Api/DTOs/Patients/
    Command : New-Item -Path "ClinicManagementAPI.Api/DTOs/Patients/UpdatePatientRequest.cs" -ItemType File -Force
    Properties:
    - FullName (string, optional, [MinLength(2)], [MaxLength(100)])
    - Email (string, optional, [EmailAddress])
    - Phone (string, optional, [Phone])
    - DateOfBirth (DateOnly, optional)
    - Gender (Gender enum, optional)
    - Address (string, optional, [MaxLength(250)])

    ⚠️ All fields are optional for partial update
    BUT at least one field must be provided
    → Validate in PatientService: if all fields are null → Result.Failure("At least one field must be provided", 400)

[x] Create PatientResponse DTO in Api/DTOs/Patients/
    Command : New-Item -Path "ClinicManagementAPI.Api/DTOs/Patients/PatientResponse.cs" -ItemType File -Force
    Properties:
    - Id (int)
    - FullName (string)
    - Email (string)
    - Phone (string)
    - DateOfBirth (DateOnly)
    - Gender (string)
    - Address (string)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime, nullable)

    ⚠️ No IsDeleted or DeletedAt in response — internal fields only
```

**Why DTOs?**

```markdown
Expose raw model → Risk of exposing sensitive fields (UserId, IsDeleted) + tight coupling ❌
Use DTOs         → Full control over what goes in and out of the API ✅
```

---

## Section 4 — Pagination + Search DTOs

**Expected Time: 20 minutes**  
**Goal:** Reusable pagination and search DTOs — used by Patients now and Doctors/Appointments later

```markdown
[x] Create PaginationRequest DTO in Api/DTOs/
    Command : New-Item -Path "ClinicManagementAPI.Api/DTOs/PaginationRequest.cs" -ItemType File -Force
    Properties:
    - Page (int, default = 1, min = 1)
    - PageSize (int, default = 10, min = 1, max = 50)
    - SearchTerm (string, optional)
      → Will search by FullName, Email, or Phone

[x] Create PagedResponse<T> DTO in Api/DTOs/
    Command : New-Item -Path "ClinicManagementAPI.Api/DTOs/PagedResponse.cs" -ItemType File -Force
    Properties:
    - Items (IEnumerable<T>)
    - Page (int)
    - PageSize (int)
    - TotalCount (int)
    - TotalPages (int)
```

**Why SearchTerm in PaginationRequest?**

```markdown
No search → Receptionist scrolls through all patients to find one → Slow UX ❌
SearchTerm → Type a name, email, or phone → Instant results ✅
Reusable   → Same DTO works for Doctors and Appointments later ✅
```

---

## Section 5 — IPatientService Interface

**Expected Time: 15 minutes**  
**Goal:** Define the contract before implementation — consistent with Result Pattern from Sprint 2

```markdown
[x] Create IPatientService interface in Core/Interfaces/
    Command : New-Item -Path "ClinicManagementAPI.Core/Interfaces/IPatientService.cs" -ItemType File -Force
    Methods (all return Result<T>):
    - Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(PaginationRequest pagination)
    - Task<Result<PatientResponse>> GetByIdAsync(int id)
    - Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request)
    - Task<Result<PatientResponse>> UpdateAsync(int id, UpdatePatientRequest request)
    - Task<Result<bool>> DeleteAsync(int id)
```

---

## Section 6 — PatientService

**Expected Time: 45 minutes**  
**Goal:** Implement business logic using Result Pattern — Soft Delete instead of Hard Delete

```markdown
[x] Create PatientService in Core/Services/
    Implements IPatientService

    GetAllAsync:
    - Start with IQueryable<Patient>
    - ⚠️ Global Query Filter already excludes IsDeleted = true (no manual filter needed)
    - If SearchTerm is provided → filter by FullName, Email, or Phone (case-insensitive Contains)
    - Get TotalCount before pagination
    - Apply Skip and Take based on pagination
    - Order by FullName
    - Map Patient → PatientResponse
    - Return Result.Success(new PagedResponse<PatientResponse> { ... })

    GetByIdAsync:
    - Find patient by Id
    - ⚠️ Global Query Filter already excludes deleted patients
    - If null → return Result.Failure("Patient not found", 404)
    - Return Result.Success(patientResponse)

    CreateAsync:
    - Check if email already exists (among ALL patients including deleted):
      context.Patients.IgnoreQueryFilters().AnyAsync(p => p.Email == email)
      → if exists, return Result.Failure("Email already registered", 400)
    - Map CreatePatientRequest → Patient
    - Set CreatedAt = DateTime.UtcNow
    - Save to Database
    - Return Result.Success(patientResponse)

    UpdateAsync:
    - Check if all fields are null
      → if all null, return Result.Failure("At least one field must be provided", 400)
    - Find patient by Id
      → if null, return Result.Failure("Patient not found", 404)
    - If email is being changed, check for duplicates (IgnoreQueryFilters, exclude self)
      → if exists, return Result.Failure("Email already registered", 400)
    - Update only provided fields (partial update)
    - Set UpdatedAt = DateTime.UtcNow
    - Save to Database
    - Return Result.Success(patientResponse)

    DeleteAsync (Soft Delete):
    - Find patient by Id
      → if null, return Result.Failure("Patient not found", 404)
    - ⚠️ Do NOT remove from database
    - Set IsDeleted = true
    - Set DeletedAt = DateTime.UtcNow
    - Save to Database
    - Return Result.Success(true)

[x] Register IPatientService in Program.cs
    builder.Services.AddScoped<IPatientService, PatientService>()
```

**Why IgnoreQueryFilters for email check?**

```markdown
Check only active patients → Deleted patient's email can be reused → Data conflict if restored ❌
Check ALL patients         → Email is truly unique across entire database → Safe ✅
```

---

## Section 7 — Patient Endpoints + Role Authorization

**Expected Time: 45 minutes**  
**Goal:** Expose CRUD endpoints with Role-based protection — first real use of Roles from Sprint 2

```markdown
[ ] Create PatientEndpoints.cs in Api/Endpoints/

    GET /api/patients
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts: page, pageSize, searchTerm as query parameters
    - Returns 200 + PagedResponse<PatientResponse>
    - Returns 401 if no token
    - Returns 403 if wrong role

    GET /api/patients/{id}
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Returns 200 + PatientResponse
    - Returns 404 if not found (or soft-deleted)
    - Returns 401 if no token
    - Returns 403 if wrong role

    POST /api/patients
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts CreatePatientRequest
    - Returns 201 + PatientResponse on success
    - Returns 400 on validation failure or duplicate email
    - Returns 401 if no token
    - Returns 403 if wrong role

    PUT /api/patients/{id}
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts UpdatePatientRequest
    - Returns 200 + PatientResponse on success
    - Returns 400 if no fields provided or duplicate email
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if wrong role

    DELETE /api/patients/{id}
    - Requires JWT Token
    - Allowed roles: Admin ONLY
    - Returns 204 on success (soft delete — data preserved)
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if wrong role

[ ] Map Patient endpoints in Program.cs
    app.MapPatientEndpoints()
```

---

## Section 8 — Assign Role Endpoint

**Expected Time: 30 minutes**  
**Goal:** Allow Admin to promote any user to a different role

```markdown
[ ] Create AssignRoleRequest DTO in Api/DTOs/Auth/
    Properties:
    - Role (string, [Required]) → must match one of AppRoles constants

[ ] Add AssignRoleAsync to IAuthService in Core/Interfaces/
    Method:
    - Task<Result<bool>> AssignRoleAsync(string userId, AssignRoleRequest request)

[ ] Implement AssignRoleAsync in AuthService
    - Find user by userId
      → if null, return Result.Failure("User not found", 404)
    - Validate role exists in AppRoles
      → if invalid, return Result.Failure("Invalid role", 400)
    - Remove current roles
    - Assign new role
    - Return Result.Success(true)

    // ⚠️ Design Decision: Each user has exactly ONE role at a time.
    // We remove all current roles before assigning the new one.
    // This is intentional for a clinic system where a person is either
    // Admin, Receptionist, or Patient — not multiple roles at once.

[ ] Add Assign Role endpoint in AuthEndpoints.cs
    PUT /api/users/{id}/role
    - Requires JWT Token
    - Allowed roles: Admin ONLY
    - Accepts AssignRoleRequest
    - Returns 200 on success
    - Returns 404 if user not found
    - Returns 400 if invalid role
    - Returns 401 if no token
    - Returns 403 if not Admin
```

---

## Section 9 — Tests

**Expected Time: 60 minutes**  
**Goal:** 70%+ coverage — verify CRUD logic, Soft Delete, search, and Authorization rules

```markdown
[ ] Create Unit Tests in Tests/Unit/PatientServiceTests.cs
    Test cases:
    — GetAll + Search:
    - GetAllAsync_ReturnsPagedPatients
    - GetAllAsync_WithPage2_ReturnsCorrectPatients
    - GetAllAsync_WithSearchTerm_FiltersCorrectly
    - GetAllAsync_WithNoResults_ReturnsEmptyList
    - GetAllAsync_DoesNotReturnDeletedPatients

    — GetById:
    - GetByIdAsync_WithValidId_ReturnsSuccessResult
    - GetByIdAsync_WithInvalidId_ReturnsFailureResult
    - GetByIdAsync_WithDeletedPatient_ReturnsFailureResult

    — Create:
    - CreateAsync_WithValidData_ReturnsSuccessResult
    - CreateAsync_WithDuplicateEmail_ReturnsFailureResult
    - CreateAsync_WithDeletedPatientEmail_ReturnsFailureResult

    — Update:
    - UpdateAsync_WithValidId_ReturnsSuccessResult
    - UpdateAsync_WithInvalidId_ReturnsFailureResult
    - UpdateAsync_WithAllFieldsNull_ReturnsFailureResult
    - UpdateAsync_WithDuplicateEmail_ReturnsFailureResult

    — Delete (Soft Delete):
    - DeleteAsync_WithValidId_SetsIsDeletedTrue
    - DeleteAsync_WithValidId_SetsDeletedAtToUtcNow
    - DeleteAsync_WithInvalidId_ReturnsFailureResult
    - DeleteAsync_PatientDisappearsFromGetAll

[ ] Add to existing Tests/Unit/AuthServiceTests.cs
    Test cases:
    - AssignRoleAsync_WithValidUserId_ReturnsSuccessResult
    - AssignRoleAsync_WithInvalidUserId_ReturnsFailureResult
    - AssignRoleAsync_WithInvalidRole_ReturnsFailureResult

[ ] Create Integration Tests in Tests/Integration/PatientEndpointsTests.cs
    Test cases:
    — Authorization tests:
    - GET    /api/patients        → 200 with Admin token
    - GET    /api/patients        → 200 with Receptionist token
    - GET    /api/patients        → 401 without token
    - GET    /api/patients        → 403 with Patient token
    - DELETE /api/patients/{id}   → 204 with Admin token
    - DELETE /api/patients/{id}   → 403 with Receptionist token

    — CRUD tests:
    - GET    /api/patients        → 200 with correct pagination
    - GET    /api/patients?searchTerm=ahmed → 200 with filtered results
    - GET    /api/patients/{id}   → 200 with valid id
    - GET    /api/patients/{id}   → 404 with invalid id
    - POST   /api/patients        → 201 with valid data
    - POST   /api/patients        → 400 with missing fields
    - POST   /api/patients        → 400 with duplicate email
    - PUT    /api/patients/{id}   → 200 with valid partial data
    - PUT    /api/patients/{id}   → 400 with all fields empty
    - PUT    /api/patients/{id}   → 404 with invalid id

    — Soft Delete tests:
    - DELETE /api/patients/{id}   → 204 returns success
    - GET    /api/patients/{id}   → 404 after soft delete
    - GET    /api/patients        → soft-deleted patient not in list
    - POST   /api/patients        → 400 creating patient with deleted patient's email

[ ] Add to existing Tests/Integration/AuthEndpointsTests.cs
    Test cases:
    - PUT /api/users/{id}/role → 200 with Admin token + valid role
    - PUT /api/users/{id}/role → 400 with invalid role
    - PUT /api/users/{id}/role → 403 with Receptionist token
    - PUT /api/users/{id}/role → 404 with invalid userId

[ ] Run all tests and verify they pass
    Command: dotnet test --verbosity normal

[ ] Check coverage
    Command: dotnet test --collect:"XPlat Code Coverage"
```

---

## Section 10 — CI Update

**Expected Time: 10 minutes**  
**Goal:** CI pipeline runs all tests including new Patient, Soft Delete, and Assign Role tests

```markdown
[ ] Push to GitHub and verify:
    ✅ Build passes
    ✅ All Auth tests still pass
    ✅ All Patient tests pass (including Soft Delete tests)
    ✅ Assign Role tests pass
    ✅ Coverage is 70%+
```

---

## Sprint 3 — Done Definition

```markdown
✅ ISoftDeletable interface is ready in Core/Interfaces/
✅ Patient model implements ISoftDeletable (IsDeleted + DeletedAt)
✅ Patient has optional UserId (FK to ApplicationUser)
✅ Email has Filtered Unique Index at database level (IsDeleted = 0)
✅ Global Query Filter automatically excludes deleted patients
✅ Email uniqueness check uses IgnoreQueryFilters (prevents reuse of deleted emails)
✅ Patient migration is applied to Database
✅ SearchTerm filters by FullName, Email, or Phone
✅ UpdatePatientRequest rejects empty requests (at least one field required)
✅ DELETE performs Soft Delete (IsDeleted = true, DeletedAt = UtcNow)
✅ GET    /api/patients           works → Admin + Receptionist only + Pagination + Search
✅ GET    /api/patients/{id}      works → Admin + Receptionist only
✅ POST   /api/patients           works → Admin + Receptionist only + Validation
✅ PUT    /api/patients/{id}      works → Admin + Receptionist only + Partial Update
✅ DELETE /api/patients/{id}      works → Admin only (Soft Delete)
✅ PUT    /api/users/{id}/role    works → Admin only (single role per user)
✅ All services use Result Pattern (no exceptions for business errors)
✅ 401 is returned for missing token
✅ 403 is returned for wrong role
✅ All Unit Tests pass (including Soft Delete + IgnoreQueryFilters tests)
✅ All Integration Tests pass
✅ CI pipeline is green on GitHub
```

---

**Next Sprint:** Doctors CRUD (with Soft Delete)
