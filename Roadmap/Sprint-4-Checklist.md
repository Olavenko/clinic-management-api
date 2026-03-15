# Sprint 4 — Doctors CRUD

**Project:** Clinic Management API  
**Sprint Duration:** 1 Week  
**Expected Time:** ~5 hours  
**Goal:** Full Doctors CRUD with Role-based Authorization and Soft Delete — same pattern as Patients  
**Prerequisites:** Sprint 3 is complete, Patients CRUD + Soft Delete are working, ISoftDeletable is ready, CI is green

---

## Section 1 — Doctor Model

**Expected Time: 20 minutes**  
**Goal:** Define the Doctor entity with Soft Delete support (reusing ISoftDeletable from Sprint 3)

```markdown
[x] Create Doctor model in Core/Models/
    Command: New-Item -Path "Core/Models/Doctor.cs" -ItemType File -Force
    Implements: ISoftDeletable
    Properties:
    - Id (int)
    - FullName (string, required)
    - Email (string, required)
    - Phone (string, required)
    - Specialization (string, required) → e.g. Cardiology, Dentistry
    - YearsOfExperience (int, required)
    - Bio (string, optional)
    - IsAvailable (bool, default = true)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime, nullable)
    - IsDeleted (bool, default = false) ← from ISoftDeletable
    - DeletedAt (DateTime, nullable)    ← from ISoftDeletable

[x] Configure Doctor entity in AppDbContext OnModelCreating
    - Filtered Unique Index on Email: .HasIndex(d => d.Email).IsUnique().HasFilter("IsDeleted = 0")
    - Global Query Filter: .HasQueryFilter(d => !d.IsDeleted)

    ⚠️ Same pattern as Patient — only active emails must be unique

[x] Add Doctors DbSet to AppDbContext
    public DbSet<Doctor> Doctors => Set<Doctor>();

[x] Add Doctor Migration
    Command: dotnet ef migrations add AddDoctors --project ClinicManagementAPI.Core
                                                 --startup-project ClinicManagementAPI.Api

[x] Apply Migration
    Command: dotnet ef database update --project ClinicManagementAPI.Core
                                       --startup-project ClinicManagementAPI.Api
```

---

## Section 2 — DTOs + Validation

**Expected Time: 20 minutes**  
**Goal:** Define request and response shapes — never expose the raw model to the API consumer

```markdown
[x] Create CreateDoctorRequest DTO in ClinicManagementAPI.Core/DTOs/Doctors/
    Command: New-Item -Path "ClinicManagementAPI.Core/DTOs/Doctors/CreateDoctorRequest.cs" -ItemType File -Force
    Properties:
    - FullName (string, [Required], [MinLength(2)], [MaxLength(100)])
    - Email (string, [Required], [EmailAddress])
    - Phone (string, [Required], [Phone])
    - Specialization (string, [Required], [MaxLength(100)])
    - YearsOfExperience (int, [Required], [Range(0, 60)])
    - Bio (string, optional, [MaxLength(500)])

[x] Create UpdateDoctorRequest DTO in ClinicManagementAPI.Core/DTOs/Doctors/
    Command: New-Item -Path "ClinicManagementAPI.Core/DTOs/Doctors/UpdateDoctorRequest.cs" -ItemType File -Force
    Properties:
    - FullName (string, optional, [MinLength(2)], [MaxLength(100)])
    - Email (string, optional, [EmailAddress])
    - Phone (string, optional, [Phone])
    - Specialization (string, optional, [MaxLength(100)])
    - YearsOfExperience (int, optional, [Range(0, 60)])
    - Bio (string, optional, [MaxLength(500)])
    - IsAvailable (bool, optional)

    ⚠️ All fields are optional for partial update
    BUT at least one field must be provided
    → Validate in DoctorService: if all fields are null → Result.Failure("At least one field must be provided", 400)

[x] Create DoctorResponse DTO in ClinicManagementAPI.Core/DTOs/Doctors/
    Command: New-Item -Path "ClinicManagementAPI.Core/DTOs/Doctors/DoctorResponse.cs" -ItemType File -Force
    Properties:
    - Id (int)
    - FullName (string)
    - Email (string)
    - Phone (string)
    - Specialization (string)
    - YearsOfExperience (int)
    - Bio (string)
    - IsAvailable (bool)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime, nullable)

    ⚠️ No IsDeleted or DeletedAt in response — internal fields only
```

---

## Section 3 — IDoctorService Interface

**Expected Time: 15 minutes**  
**Goal:** Define the contract before implementation — consistent with Result Pattern from Sprint 2

```markdown
[x] Create IDoctorService interface in ClinicManagementAPI.Core/Interfaces/
    Command: New-Item -Path "ClinicManagementAPI.Core/Interfaces/IDoctorService.cs" -ItemType File -Force
    Methods (all return Result<T>):
    - Task<Result<PagedResponse<DoctorResponse>>> GetAllAsync(PaginationRequest pagination)
    - Task<Result<PagedResponse<DoctorResponse>>> GetAvailableAsync(PaginationRequest pagination)
    - Task<Result<DoctorResponse>> GetByIdAsync(int id)
    - Task<Result<DoctorResponse>> CreateAsync(CreateDoctorRequest request)
    - Task<Result<DoctorResponse>> UpdateAsync(int id, UpdateDoctorRequest request)
    - Task<Result<bool>> DeleteAsync(int id)
```

**Why GetAvailableAsync?**

```markdown
Appointments in Sprint 5 need to know which doctors are available
Better to add it now than revisit DoctorService later ✅
```

---

## Section 4 — DoctorService

**Expected Time: 45 minutes**  
**Goal:** Implement business logic using Result Pattern — Soft Delete instead of Hard Delete

```markdown
[ ] Create DoctorService in Core/Services/
    Implements IDoctorService

    GetAllAsync:
    - Start with IQueryable<Doctor>
    - ⚠️ Global Query Filter already excludes IsDeleted = true
    - If SearchTerm is provided → filter by FullName or Specialization (case-insensitive Contains)
    - Get TotalCount before pagination
    - Apply Skip and Take based on pagination
    - Order by FullName
    - Map Doctor → DoctorResponse
    - Return Result.Success(new PagedResponse<DoctorResponse> { ... })

    GetAvailableAsync:
    - Start with IQueryable<Doctor>
    - ⚠️ Global Query Filter already excludes deleted doctors
    - Filter by IsAvailable = true
    - If SearchTerm is provided → filter by FullName or Specialization (case-insensitive Contains)
    - Get TotalCount before pagination
    - Apply Skip and Take based on pagination
    - Order by FullName
    - Map Doctor → DoctorResponse
    - Return Result.Success(new PagedResponse<DoctorResponse> { ... })

    GetByIdAsync:
    - Find doctor by Id
    - ⚠️ Global Query Filter already excludes deleted doctors
    - If null → return Result.Failure("Doctor not found", 404)
    - Return Result.Success(doctorResponse)

    CreateAsync:
    - Check if email already exists (among ALL doctors including deleted):
      context.Doctors.IgnoreQueryFilters().AnyAsync(d => d.Email == email)
      → if exists, return Result.Failure("Email already registered", 400)
    - Map CreateDoctorRequest → Doctor
    - Set CreatedAt = DateTime.UtcNow
    - Set IsAvailable = true by default
    - Save to Database
    - Return Result.Success(doctorResponse)

    UpdateAsync:
    - Check if all fields are null
      → if all null, return Result.Failure("At least one field must be provided", 400)
    - Find doctor by Id
      → if null, return Result.Failure("Doctor not found", 404)
    - If email is being changed, check for duplicates (IgnoreQueryFilters, exclude self)
      → if exists, return Result.Failure("Email already registered", 400)
    - Update only provided fields (partial update)
    - Set UpdatedAt = DateTime.UtcNow
    - Save to Database
    - Return Result.Success(doctorResponse)

    DeleteAsync (Soft Delete):
    - Find doctor by Id
      → if null, return Result.Failure("Doctor not found", 404)
    - ⚠️ Do NOT remove from database
    - Set IsDeleted = true
    - Set DeletedAt = DateTime.UtcNow
    - Save to Database
    - Return Result.Success(true)

[ ] Register IDoctorService in Program.cs
    builder.Services.AddScoped<IDoctorService, DoctorService>()
```

---

## Section 5 — Doctor Endpoints + Role Authorization

**Expected Time: 45 minutes**  
**Goal:** Expose CRUD endpoints with Role-based protection — same pattern as Patients

```markdown
[ ] Create DoctorEndpoints.cs in Api/Endpoints/

    GET /api/doctors
    - No JWT required (public endpoint — anyone can see doctors list)
    - Accepts: page, pageSize, searchTerm as query parameters
    - Returns 200 + PagedResponse<DoctorResponse>

    GET /api/doctors/available
    - No JWT required (public endpoint)
    - Accepts: page, pageSize, searchTerm as query parameters
    - Returns 200 + PagedResponse<DoctorResponse>

    GET /api/doctors/{id}
    - No JWT required (public endpoint)
    - Returns 200 + DoctorResponse
    - Returns 404 if not found (or soft-deleted)

    POST /api/doctors
    - Requires JWT Token
    - Allowed roles: Admin ONLY
    - Accepts CreateDoctorRequest
    - Returns 201 + DoctorResponse on success
    - Returns 400 on validation failure or duplicate email
    - Returns 401 if no token
    - Returns 403 if not Admin

    PUT /api/doctors/{id}
    - Requires JWT Token
    - Allowed roles: Admin ONLY
    - Accepts UpdateDoctorRequest
    - Returns 200 + DoctorResponse on success
    - Returns 400 if no fields provided or duplicate email
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if not Admin

    DELETE /api/doctors/{id}
    - Requires JWT Token
    - Allowed roles: Admin ONLY
    - Returns 204 on success (soft delete — data preserved)
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if not Admin

[ ] Map Doctor endpoints in Program.cs
    app.MapDoctorEndpoints()
```

**Why GET endpoints are public?**

```markdown
Patients need to see available doctors before booking → No login required ✅
Admin manages doctors → Login required ✅
```

---

## Section 6 — Tests

**Expected Time: 60 minutes**  
**Goal:** 70%+ coverage — verify CRUD logic, Soft Delete, search, and Authorization rules

```markdown
[ ] Create Unit Tests in Tests/Unit/DoctorServiceTests.cs
    Test cases:
    — GetAll + Search:
    - GetAllAsync_ReturnsPagedDoctors
    - GetAllAsync_WithPage2_ReturnsCorrectDoctors
    - GetAllAsync_WithSearchTerm_FiltersByNameOrSpecialization
    - GetAllAsync_WithNoResults_ReturnsEmptyList
    - GetAllAsync_DoesNotReturnDeletedDoctors
    - GetAvailableAsync_ReturnsOnlyAvailableDoctors
    - GetAvailableAsync_DoesNotReturnDeletedDoctors
    - GetAvailableAsync_WithSearchTerm_FiltersCorrectly

    — GetById:
    - GetByIdAsync_WithValidId_ReturnsSuccessResult
    - GetByIdAsync_WithInvalidId_ReturnsFailureResult
    - GetByIdAsync_WithDeletedDoctor_ReturnsFailureResult

    — Create:
    - CreateAsync_WithValidData_ReturnsSuccessResult
    - CreateAsync_WithDuplicateEmail_ReturnsFailureResult
    - CreateAsync_WithDeletedDoctorEmail_ReturnsFailureResult
    - CreateAsync_SetsIsAvailableToTrue_ByDefault

    — Update:
    - UpdateAsync_WithValidId_ReturnsSuccessResult
    - UpdateAsync_WithInvalidId_ReturnsFailureResult
    - UpdateAsync_WithAllFieldsNull_ReturnsFailureResult
    - UpdateAsync_WithDuplicateEmail_ReturnsFailureResult
    - UpdateAsync_CanSetIsAvailableToFalse

    — Delete (Soft Delete):
    - DeleteAsync_WithValidId_SetsIsDeletedTrue
    - DeleteAsync_WithValidId_SetsDeletedAtToUtcNow
    - DeleteAsync_WithInvalidId_ReturnsFailureResult
    - DeleteAsync_DoctorDisappearsFromGetAll
    - DeleteAsync_DoctorDisappearsFromGetAvailable

[ ] Create Integration Tests in Tests/Integration/DoctorEndpointsTests.cs
    Test cases:
    — Public endpoints (no token):
    - GET    /api/doctors            → 200 without token (public)
    - GET    /api/doctors?searchTerm=cardiology → 200 with filtered results
    - GET    /api/doctors/available  → 200 without token (public)
    - GET    /api/doctors/{id}       → 200 with valid id (public)
    - GET    /api/doctors/{id}       → 404 with invalid id

    — Authorization tests:
    - POST   /api/doctors            → 201 with Admin token
    - POST   /api/doctors            → 401 without token
    - POST   /api/doctors            → 403 with Receptionist token
    - PUT    /api/doctors/{id}       → 200 with Admin token
    - PUT    /api/doctors/{id}       → 403 with Receptionist token
    - DELETE /api/doctors/{id}       → 204 with Admin token
    - DELETE /api/doctors/{id}       → 403 with Receptionist token

    — Validation tests:
    - POST   /api/doctors            → 400 with missing fields
    - POST   /api/doctors            → 400 with duplicate email
    - PUT    /api/doctors/{id}       → 400 with all fields empty
    - PUT    /api/doctors/{id}       → 400 with duplicate email
    - PUT    /api/doctors/{id}       → 404 with invalid id

    — Soft Delete tests:
    - DELETE /api/doctors/{id}       → 204 returns success
    - GET    /api/doctors/{id}       → 404 after soft delete
    - GET    /api/doctors            → soft-deleted doctor not in list
    - GET    /api/doctors/available  → soft-deleted doctor not in available list
    - POST   /api/doctors            → 400 creating doctor with deleted doctor's email

[ ] Run all tests and verify they pass
    Command: dotnet test --verbosity normal

[ ] Check coverage
    Command: dotnet test --collect:"XPlat Code Coverage"
```

---

## Section 7 — CI Update

**Expected Time: 10 minutes**  
**Goal:** CI pipeline runs all tests including new Doctor and Soft Delete tests

```markdown
[ ] Push to GitHub and verify:
    ✅ Build passes
    ✅ All Auth tests still pass
    ✅ All Patient tests still pass (including Soft Delete)
    ✅ All Doctor tests pass (including Soft Delete tests)
    ✅ Coverage is 70%+
```

---

## Section 8 — Diagrams

**Expected Time: 30 minutes**  
**Goal:** Create and update necessary system diagrams for Sprint 4 features (Doctors CRUD)

```markdown
[ ] Review docs/ to determine required diagrams for Sprint 4
[ ] Update/Create Component/Sequence Diagrams for doctor services
[ ] Verify PlantUML/Markdown diagrams render correctly
```

---

## Sprint 4 — Done Definition

```markdown
✅ Doctor model implements ISoftDeletable (IsDeleted + DeletedAt)
✅ Email has Filtered Unique Index at database level (IsDeleted = 0)
✅ Global Query Filter automatically excludes deleted doctors
✅ Email uniqueness check uses IgnoreQueryFilters (prevents reuse of deleted emails)
✅ Doctor migration is applied to Database
✅ SearchTerm filters by FullName or Specialization
✅ UpdateDoctorRequest rejects empty requests (at least one field required)
✅ DELETE performs Soft Delete (IsDeleted = true, DeletedAt = UtcNow)
✅ GET    /api/doctors            works → Public + Pagination + Search
✅ GET    /api/doctors/available  works → Public + Pagination + Search
✅ GET    /api/doctors/{id}       works → Public
✅ POST   /api/doctors            works → Admin only + Validation
✅ PUT    /api/doctors/{id}       works → Admin only + Partial Update
✅ DELETE /api/doctors/{id}       works → Admin only (Soft Delete)
✅ All services use Result Pattern (no exceptions for business errors)
✅ 401 is returned for missing token on protected endpoints
✅ 403 is returned for wrong role
✅ All Unit Tests pass (including Soft Delete + IgnoreQueryFilters tests)
✅ All Integration Tests pass
✅ CI pipeline is green on GitHub
✅ Diagrams for Sprint 4 are created and updated
```

---

**Next Sprint:** Appointments + Business Logic
