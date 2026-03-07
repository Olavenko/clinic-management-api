# Sprint 5 — Appointments + Business Logic

**Project:** Clinic Management API  
**Sprint Duration:** 1 Week  
**Expected Time:** ~7 hours  
**Goal:** Full Appointments management with real Business Logic — the most complex sprint in the project  
**Prerequisites:** Sprint 4 is complete, Patients + Doctors CRUD with Soft Delete are working, CI is green

---

## Section 1 — Appointment Model

**Expected Time: 25 minutes**  
**Goal:** Define the Appointment entity with all relationships and status tracking

```markdown
[ ] Create AppointmentStatus enum in Core/Models/
    public enum AppointmentStatus
    {
        Scheduled  = 0,  // default on creation
        Completed  = 1,  // doctor marked it done
        Cancelled  = 2   // admin or receptionist cancelled it
    }

[ ] Create Appointment model in Core/Models/
    Properties:
    - Id (int)
    - PatientId (int, required, FK → Patient)
    - DoctorId (int, required, FK → Doctor)
    - AppointmentDate (DateOnly, required)
    - AppointmentTime (TimeOnly, required)
    - DurationMinutes (int, default = 30)
    - Status (AppointmentStatus, default = Scheduled)
    - Notes (string, optional)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime, nullable)

    Navigation properties:
    - Patient (Patient)
    - Doctor (Doctor)

    ⚠️ Appointments do NOT implement ISoftDeletable.
    They use Status-based lifecycle instead:
    - Scheduled → Completed/Cancelled
    - Only Cancelled can be hard deleted
    - This preserves audit trail without needing soft delete

[ ] Configure Appointment entity in AppDbContext OnModelCreating
    - PatientId FK: .HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict)
    - DoctorId FK: .HasOne(a => a.Doctor).WithMany().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict)

    ⚠️ Why Restrict (not Cascade)?
    - Cascade → Deleting a doctor deletes all their appointments → Destroys medical history ❌
    - Restrict → Database blocks deletion if appointments exist → Safe ✅
    - With Soft Delete on Patient/Doctor → they're never truly deleted → Restrict is safe ✅

[ ] Add Appointments DbSet to AppDbContext
    public DbSet<Appointment> Appointments => Set<Appointment>();

[ ] Add Appointment Migration
    Command: dotnet ef migrations add AddAppointments --project ClinicManagementAPI.Core
                                                      --startup-project ClinicManagementAPI.Api

[ ] Apply Migration
    Command: dotnet ef database update --project ClinicManagementAPI.Core
                                       --startup-project ClinicManagementAPI.Api
```

---

## Section 2 — DTOs + Validation

**Expected Time: 30 minutes**  
**Goal:** Define request and response shapes with clear validation rules

```markdown
[ ] Create AppointmentFilterRequest DTO in Api/DTOs/Appointments/
    Inherits or wraps PaginationRequest (Page, PageSize, SearchTerm)
    Extra Properties:
    - DateFrom (DateOnly, optional) → filter appointments from this date
    - DateTo (DateOnly, optional) → filter appointments until this date
    - Status (AppointmentStatus, optional) → filter by status

    ⚠️ SearchTerm filters by PatientName or DoctorName
    ⚠️ DateFrom and DateTo are inclusive

[ ] Create CreateAppointmentRequest DTO in Api/DTOs/Appointments/
    Properties:
    - PatientId (int, [Required])
    - DoctorId (int, [Required])
    - AppointmentDate (DateOnly, [Required])
    - AppointmentTime (TimeOnly, [Required])
    - DurationMinutes (int, optional, [Range(15, 120)], default = 30)
    - Notes (string, optional, [MaxLength(500)])

[ ] Create UpdateAppointmentRequest DTO in Api/DTOs/Appointments/
    Properties:
    - AppointmentDate (DateOnly, optional)
    - AppointmentTime (TimeOnly, optional)
    - DurationMinutes (int, optional, [Range(15, 120)])
    - Notes (string, optional, [MaxLength(500)])

    ⚠️ No PatientId or DoctorId — cannot change who the appointment belongs to
    ⚠️ At least one field must be provided
    → Validate in AppointmentService: if all fields are null → Result.Failure(400)

[ ] Create UpdateAppointmentStatusRequest DTO in Api/DTOs/Appointments/
    Properties:
    - Status (AppointmentStatus, [Required])

[ ] Create AppointmentResponse DTO in Api/DTOs/Appointments/
    Properties:
    - Id (int)
    - PatientId (int)
    - PatientName (string)
    - DoctorId (int)
    - DoctorName (string)
    - DoctorSpecialization (string)
    - AppointmentDate (DateOnly)
    - AppointmentTime (TimeOnly)
    - DurationMinutes (int)
    - Status (string)
    - Notes (string)
    - CreatedAt (DateTime)
    - UpdatedAt (DateTime, nullable)
```

**Why no PatientId/DoctorId in UpdateAppointmentRequest?**

```markdown
Changing patient or doctor → Creates a new appointment, not an update ❌
Only date/time can change  → Keeps appointment history clean ✅
```

---

## Section 3 — IAppointmentService Interface

**Expected Time: 15 minutes**  
**Goal:** Define the contract before implementation — consistent with Result Pattern

```markdown
[ ] Create IAppointmentService interface in Core/Interfaces/
    Methods (all return Result<T>):
    - Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(AppointmentFilterRequest filter)
    - Task<Result<PagedResponse<AppointmentResponse>>> GetByPatientAsync(int patientId, PaginationRequest pagination)
    - Task<Result<PagedResponse<AppointmentResponse>>> GetByDoctorAsync(int doctorId, PaginationRequest pagination)
    - Task<Result<AppointmentResponse>> GetByIdAsync(int id)
    - Task<Result<AppointmentResponse>> CreateAsync(CreateAppointmentRequest request)
    - Task<Result<AppointmentResponse>> UpdateAsync(int id, UpdateAppointmentRequest request)
    - Task<Result<AppointmentResponse>> UpdateStatusAsync(int id, UpdateAppointmentStatusRequest request)
    - Task<Result<bool>> DeleteAsync(int id)
```

**Why GetByPatient and GetByDoctor?**

```markdown
Admin/Receptionist needs to see all appointments for a specific patient or doctor
Better to add now than revisit AppointmentService in a future sprint ✅
```

---

## Section 4 — AppointmentService + Business Logic

**Expected Time: 90 minutes**  
**Goal:** Implement all business rules — this is the most important section in the entire project

### Overlap Detection Formula

Before implementing, understand how overlap detection works:

```markdown
Two appointments overlap when:
  existingStart < newEnd  AND  newStart < existingEnd

Where:
  newStart     = AppointmentTime
  newEnd       = AppointmentTime + DurationMinutes
  existingStart = existing.AppointmentTime
  existingEnd   = existing.AppointmentTime + existing.DurationMinutes

Example:
  Existing: 10:00 → 10:30 (30 min)
  New:      10:15 → 10:45 (30 min)
  Check:    10:00 < 10:45 ✅ AND 10:15 < 10:30 ✅ → OVERLAP ❌

  Existing: 10:00 → 10:30 (30 min)
  New:      10:30 → 11:00 (30 min)
  Check:    10:00 < 11:00 ✅ AND 10:30 < 10:30 ❌ → NO OVERLAP ✅ (back-to-back is fine)
```

### Soft Delete Interaction

```markdown
⚠️ Important: Patient and Doctor have Global Query Filters (IsDeleted = false).
When loading appointments with .Include(a => a.Patient) and .Include(a => a.Doctor):
- EF Core automatically applies the filter → soft-deleted patient/doctor returns null in navigation
- For existing appointments: use .IgnoreQueryFilters() on the Include OR load Patient/Doctor separately
- For creating new appointments: the normal query (with filter) is correct — you can't book a deleted patient/doctor

Recommended approach for GetAllAsync / GetByIdAsync:
- Load appointments normally (appointments don't have query filter)
- Patient/Doctor navigation properties might be null if soft-deleted
- Handle null navigation gracefully: PatientName = appointment.Patient?.FullName ?? "Deleted Patient"
```

### Implementation

```markdown
[ ] Create AppointmentService in Core/Services/
    Implements IAppointmentService

    GetAllAsync:
    - Include Patient and Doctor (eager loading)
    - ⚠️ Handle soft-deleted Patient/Doctor in navigation (see Soft Delete Interaction above)
    - If SearchTerm is provided → filter by PatientName or DoctorName (case-insensitive Contains)
    - If DateFrom is provided → filter by AppointmentDate >= DateFrom
    - If DateTo is provided → filter by AppointmentDate <= DateTo
    - If Status is provided → filter by Status
    - Get TotalCount before pagination
    - Apply pagination
    - Order by AppointmentDate, then AppointmentTime
    - Return Result.Success(pagedResponse)

    GetByPatientAsync:
    - Validate patient exists (normal query — Global Filter checks IsDeleted)
      → if null, return Result.Failure("Patient not found", 404)
    - Filter by PatientId
    - Include Doctor (eager loading)
    - Apply pagination
    - Return Result.Success(pagedResponse)

    GetByDoctorAsync:
    - Validate doctor exists (normal query — Global Filter checks IsDeleted)
      → if null, return Result.Failure("Doctor not found", 404)
    - Filter by DoctorId
    - Include Patient (eager loading)
    - Apply pagination
    - Return Result.Success(pagedResponse)

    GetByIdAsync:
    - Find appointment by Id (include Patient and Doctor)
    - If null → return Result.Failure("Appointment not found", 404)
    - Return Result.Success(appointmentResponse)

    CreateAsync:
    ⚠️ Business Rules — all must pass before saving:

    Rule 1 — Patient must exist (and not soft-deleted)
    - Find patient by PatientId (normal query — Global Filter applies)
      → if null, return Result.Failure("Patient not found", 404)

    Rule 2 — Doctor must exist (and not soft-deleted) and be available
    - Find doctor by DoctorId (normal query — Global Filter applies)
      → if null, return Result.Failure("Doctor not found", 404)
      → if IsAvailable = false, return Result.Failure("Doctor is not available", 400)

    Rule 3 — Appointment cannot be in the past
    - If AppointmentDate < today
      → return Result.Failure("Appointment date cannot be in the past", 400)

    Rule 4 — Patient has no overlapping appointment
    - Check Scheduled appointments for same patient on same date
    - Use overlap formula: existingStart < newEnd AND newStart < existingEnd
      → if overlap found, return Result.Failure("Patient already has an appointment at this time", 400)

    Rule 5 — Doctor has no overlapping appointment
    - Check Scheduled appointments for same doctor on same date
    - Use overlap formula: existingStart < newEnd AND newStart < existingEnd
      → if overlap found, return Result.Failure("Doctor already has an appointment at this time", 400)

    - If all rules pass → Save appointment with Status = Scheduled
    - Return Result.Success(appointmentResponse)

    UpdateAsync:
    ⚠️ Business Rules:

    Rule 1 — At least one field must be provided
    - If all fields are null
      → return Result.Failure("At least one field must be provided", 400)

    Rule 2 — Appointment must exist
    - Find appointment by Id
      → if null, return Result.Failure("Appointment not found", 404)

    Rule 3 — Only Scheduled appointments can be updated
    - If Status != Scheduled
      → return Result.Failure("Only scheduled appointments can be updated", 400)

    Rule 4 — New date cannot be in the past (only if date is being changed)
    - If new AppointmentDate is provided AND < today
      → return Result.Failure("Appointment date cannot be in the past", 400)

    Rule 5 — Check overlapping (only if date, time, or duration changed)
    - Only run conflict check if AppointmentDate, AppointmentTime, or DurationMinutes changed
    - Use the final values (new if provided, existing if not) for overlap calculation
    - ⚠️ EXCLUDE the current appointment from the conflict query (WHERE Id != currentId)
    - Re-check both patient and doctor conflicts
      → return Result.Failure if conflict found

    - If all rules pass → Update and save
    - Set UpdatedAt = DateTime.UtcNow
    - Return Result.Success(appointmentResponse)

    UpdateStatusAsync:
    ⚠️ Business Rules:

    Rule 1 — Appointment must exist
    - Find appointment by Id
      → if null, return Result.Failure("Appointment not found", 404)

    Rule 2 — Status transition rules
    - Scheduled  → Completed  ✅ allowed
    - Scheduled  → Cancelled  ✅ allowed
    - Completed  → anything   ❌ return Result.Failure("Completed appointments cannot be changed", 400)
    - Cancelled  → anything   ❌ return Result.Failure("Cancelled appointments cannot be changed", 400)

    - If transition is valid → Update status and save
    - Set UpdatedAt = DateTime.UtcNow
    - Return Result.Success(appointmentResponse)

    DeleteAsync:
    ⚠️ Business Rules:

    Rule 1 — Appointment must exist
    - Find appointment by Id
      → if null, return Result.Failure("Appointment not found", 404)

    Rule 2 — Only Cancelled appointments can be deleted
    - If Status != Cancelled
      → return Result.Failure("Only cancelled appointments can be deleted", 400)

    - If rule passes → Hard delete (not soft delete — status lifecycle handles audit trail)
    - Return Result.Success(true)

[ ] Register IAppointmentService in Program.cs
    builder.Services.AddScoped<IAppointmentService, AppointmentService>()
```

**Why hard delete for Appointments?**

```markdown
Appointments have status lifecycle → Scheduled → Cancelled → then deletable
Cancel preserves the audit trail → delete only removes cancelled records
Soft Delete would be redundant here → status already tracks the lifecycle ✅
```

**Why only Cancelled appointments can be deleted?**

```markdown
Deleting Scheduled/Completed → Destroys medical history ❌
Cancel first, then delete    → Audit trail is preserved ✅
```

---

## Section 5 — Appointment Endpoints + Role Authorization

**Expected Time: 45 minutes**  
**Goal:** Expose all Appointment endpoints with correct Role protection

```markdown
[ ] Create AppointmentEndpoints.cs in Api/Endpoints/

    GET /api/appointments
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts: page, pageSize, searchTerm, dateFrom, dateTo, status as query parameters
    - Returns 200 + PagedResponse<AppointmentResponse>
    - Returns 401 if no token
    - Returns 403 if wrong role

    GET /api/appointments/{id}
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Returns 200 + AppointmentResponse
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if wrong role

    GET /api/appointments/patient/{patientId}
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts: page, pageSize as query parameters
    - Returns 200 + PagedResponse<AppointmentResponse>
    - Returns 404 if patient not found (or soft-deleted)
    - Returns 401 if no token
    - Returns 403 if wrong role

    GET /api/appointments/doctor/{doctorId}
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts: page, pageSize as query parameters
    - Returns 200 + PagedResponse<AppointmentResponse>
    - Returns 404 if doctor not found (or soft-deleted)
    - Returns 401 if no token
    - Returns 403 if wrong role

    POST /api/appointments
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts CreateAppointmentRequest
    - Returns 201 + AppointmentResponse on success
    - Returns 400 on validation failure or business rule violation
    - Returns 401 if no token
    - Returns 403 if wrong role

    PUT /api/appointments/{id}
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts UpdateAppointmentRequest
    - Returns 200 + AppointmentResponse on success
    - Returns 400 if empty request, not Scheduled, past date, or conflict
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if wrong role

    PATCH /api/appointments/{id}/status
    - Requires JWT Token
    - Allowed roles: Admin, Receptionist
    - Accepts UpdateAppointmentStatusRequest
    - Returns 200 + AppointmentResponse on success
    - Returns 400 if status transition is invalid
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if wrong role

    DELETE /api/appointments/{id}
    - Requires JWT Token
    - Allowed roles: Admin ONLY
    - Returns 204 on success (hard delete — only Cancelled appointments)
    - Returns 400 if appointment is not Cancelled
    - Returns 404 if not found
    - Returns 401 if no token
    - Returns 403 if not Admin

[ ] Map Appointment endpoints in Program.cs
    app.MapAppointmentEndpoints()
```

---

## Section 6 — Tests

**Expected Time: 75 minutes**  
**Goal:** 70%+ coverage — Business Logic rules and Soft Delete interaction must be fully tested

```markdown
[ ] Create Unit Tests in Tests/Unit/AppointmentServiceTests.cs

    — CreateAsync tests (Business Rules):
    - CreateAsync_WithValidData_ReturnsSuccessResult
    - CreateAsync_WithNonExistentPatient_ReturnsFailureResult
    - CreateAsync_WithSoftDeletedPatient_ReturnsFailureResult
    - CreateAsync_WithNonExistentDoctor_ReturnsFailureResult
    - CreateAsync_WithSoftDeletedDoctor_ReturnsFailureResult
    - CreateAsync_WithUnavailableDoctor_ReturnsFailureResult
    - CreateAsync_WithPastDate_ReturnsFailureResult
    - CreateAsync_WithPatientConflict_ReturnsFailureResult
    - CreateAsync_WithDoctorConflict_ReturnsFailureResult
    - CreateAsync_WithBackToBackAppointments_ReturnsSuccessResult

    — GetAll + Filter tests:
    - GetAllAsync_ReturnsPagedAppointments
    - GetAllAsync_WithSearchTerm_FiltersByPatientOrDoctorName
    - GetAllAsync_WithDateRange_FiltersCorrectly
    - GetAllAsync_WithStatusFilter_FiltersCorrectly
    - GetAllAsync_ShowsAppointmentsWithSoftDeletedPatient (graceful null handling)
    - GetByPatientAsync_WithValidId_ReturnsPatientAppointments
    - GetByPatientAsync_WithSoftDeletedPatient_ReturnsFailureResult
    - GetByPatientAsync_WithInvalidId_ReturnsFailureResult
    - GetByDoctorAsync_WithValidId_ReturnsDoctorAppointments
    - GetByDoctorAsync_WithSoftDeletedDoctor_ReturnsFailureResult
    - GetByDoctorAsync_WithInvalidId_ReturnsFailureResult

    — UpdateAsync tests:
    - UpdateAsync_WithValidData_ReturnsSuccessResult
    - UpdateAsync_WithInvalidId_ReturnsFailureResult
    - UpdateAsync_WithAllFieldsNull_ReturnsFailureResult
    - UpdateAsync_WithCompletedAppointment_ReturnsFailureResult
    - UpdateAsync_WithCancelledAppointment_ReturnsFailureResult
    - UpdateAsync_WithPastDate_ReturnsFailureResult
    - UpdateAsync_WithConflict_ReturnsFailureResult
    - UpdateAsync_SameTimeNoChange_DoesNotTriggerConflict

    — UpdateStatusAsync tests (Status Transition Rules):
    - UpdateStatusAsync_ScheduledToCompleted_ReturnsSuccessResult
    - UpdateStatusAsync_ScheduledToCancelled_ReturnsSuccessResult
    - UpdateStatusAsync_CompletedToAny_ReturnsFailureResult
    - UpdateStatusAsync_CancelledToAny_ReturnsFailureResult

    — DeleteAsync tests:
    - DeleteAsync_WithCancelledAppointment_ReturnsSuccessResult
    - DeleteAsync_WithScheduledAppointment_ReturnsFailureResult
    - DeleteAsync_WithCompletedAppointment_ReturnsFailureResult
    - DeleteAsync_WithInvalidId_ReturnsFailureResult

[ ] Create Integration Tests in Tests/Integration/AppointmentEndpointsTests.cs
    Test cases:
    — Authorization tests:
    - GET    /api/appointments              → 200 with Admin token
    - GET    /api/appointments              → 200 with Receptionist token
    - GET    /api/appointments              → 401 without token
    - GET    /api/appointments              → 403 with Patient token
    - DELETE /api/appointments/{id}         → 204 with Admin token
    - DELETE /api/appointments/{id}         → 403 with Receptionist token

    — Filter tests:
    - GET    /api/appointments?searchTerm=ahmed         → 200 with filtered results
    - GET    /api/appointments?dateFrom=2026-03-01&dateTo=2026-03-31 → 200 with date range
    - GET    /api/appointments?status=Scheduled         → 200 with status filter
    - GET    /api/appointments/patient/{id}             → 200 with valid patient
    - GET    /api/appointments/patient/{id}             → 404 with soft-deleted patient
    - GET    /api/appointments/doctor/{id}              → 200 with valid doctor
    - GET    /api/appointments/doctor/{id}              → 404 with soft-deleted doctor

    — Business rule tests:
    - POST   /api/appointments              → 201 with valid data
    - POST   /api/appointments              → 400 with past date
    - POST   /api/appointments              → 400 with unavailable doctor
    - POST   /api/appointments              → 400 with soft-deleted patient
    - POST   /api/appointments              → 400 with patient conflict
    - POST   /api/appointments              → 400 with doctor conflict
    - PUT    /api/appointments/{id}         → 200 with valid update
    - PUT    /api/appointments/{id}         → 400 with empty request
    - PUT    /api/appointments/{id}         → 400 with completed appointment
    - PATCH  /api/appointments/{id}/status  → 200 Scheduled → Completed
    - PATCH  /api/appointments/{id}/status  → 200 Scheduled → Cancelled
    - PATCH  /api/appointments/{id}/status  → 400 Completed → Cancelled
    - DELETE /api/appointments/{id}         → 204 with Cancelled appointment
    - DELETE /api/appointments/{id}         → 400 with Scheduled appointment

[ ] Run all tests and verify they pass
    Command: dotnet test --verbosity normal

[ ] Check coverage
    Command: dotnet test --collect:"XPlat Code Coverage"
```

---

## Section 7 — CI Update

**Expected Time: 10 minutes**  
**Goal:** CI pipeline runs all tests including Appointment and Soft Delete interaction tests

```markdown
[ ] Push to GitHub and verify:
    ✅ Build passes
    ✅ All Auth tests still pass
    ✅ All Patient tests still pass (including Soft Delete)
    ✅ All Doctor tests still pass (including Soft Delete)
    ✅ All Appointment tests pass (including business rule + soft delete interaction tests)
    ✅ Coverage is 70%+
```

---

## Sprint 5 — Done Definition

```markdown
✅ Appointment model with FK relationships (Restrict delete) applied to Database
✅ Appointments use Status lifecycle (not Soft Delete) — by design
✅ Overlap detection uses correct formula: existingStart < newEnd AND newStart < existingEnd
✅ Soft-deleted patients/doctors cannot book new appointments (Global Query Filter)
✅ Existing appointments gracefully handle soft-deleted patient/doctor names
✅ GET    /api/appointments                works → Admin + Receptionist + Search + Date Filter + Status Filter
✅ GET    /api/appointments/{id}           works → Admin + Receptionist only
✅ GET    /api/appointments/patient/{id}   works → Admin + Receptionist only (404 for soft-deleted patient)
✅ GET    /api/appointments/doctor/{id}    works → Admin + Receptionist only (404 for soft-deleted doctor)
✅ POST   /api/appointments               works → All 5 business rules enforced (including soft delete check)
✅ PUT    /api/appointments/{id}          works → Only Scheduled + empty check + smart conflict check
✅ PATCH  /api/appointments/{id}/status   works → Status transition rules enforced
✅ DELETE /api/appointments/{id}          works → Only Cancelled can be hard deleted + Admin only
✅ Update conflict check excludes current appointment (no self-conflict)
✅ Update conflict check only runs when date/time/duration actually changed
✅ All services use Result Pattern (no exceptions for business errors)
✅ All Unit Tests pass (including overlap, self-exclude, soft delete interaction tests)
✅ All Integration Tests pass
✅ CI pipeline is green on GitHub
```

---

**Next Sprint:** Polish + Documentation + Coverage Review
