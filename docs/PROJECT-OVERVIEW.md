# Project Overview — Clinic Management API

## What Is This Project?

A RESTful API for managing a medical clinic's daily operations: patient registration, doctor management, and appointment booking. Built as a portfolio project to demonstrate backend engineering skills with real business logic — not just CRUD.

## Problem Statement

A clinic needs a system where:

- **Receptionists** can register patients, manage appointments, and view schedules
- **Admins** can manage everything including doctors, roles, and deletions
- **Patients** can view available doctors (public access)
- Appointments must respect real-world constraints: no double-booking, no past dates, no overlapping schedules

## Tech Stack — Why Each Choice

| Technology                | Why This Over Alternatives                                                                                                                                                        |
| :------------------------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **.NET 10 / C#**          | Enterprise-grade, strong typing, excellent tooling. Dominant in Egyptian enterprise market and remote positions                                                                   |
| **Minimal API**           | Lighter than Controllers for a focused API. Less ceremony, same capabilities. Better fit for Vertical Slice thinking                                                              |
| **EF Core + SQL Server**  | Industry standard ORM. Code-first migrations, LINQ-to-SQL, Global Query Filters for soft delete                                                                                   |
| **ASP.NET Core Identity** | Microsoft-maintained auth system. Handles password hashing, token management, role management — no need to reinvent security                                                      |
| **JWT + Refresh Token**   | Stateless authentication. Access token (60 min) + Refresh token (7 days) for seamless UX without frequent logins                                                                  |
| **Result Pattern**        | Expected business errors (wrong password, duplicate email) return `Result.Failure` instead of throwing exceptions. Exceptions reserved for unexpected errors (DB crash, null ref) |
| **xUnit**                 | Most popular .NET testing framework. Clean syntax, parallel execution, strong community                                                                                           |
| **GitHub Actions CI**     | Free for public repos. Runs build + tests on every push. Branch protection ensures main is always green                                                                           |
| **Scalar UI**             | Modern OpenAPI viewer (replaces Swagger UI). Better UX, built-in JWT auth testing                                                                                                 |

## Architecture Decisions

### Layered Architecture — 3-Project Solution

```text
ClinicManagementAPI/
├── ClinicManagementAPI.Api/       → Web layer (Endpoints, DTOs, Middleware)
├── ClinicManagementAPI.Core/      → Business layer (Models, Services, Interfaces, Data)
├── ClinicManagementAPI.Tests/     → Test layer (Unit + Integration)
├── Directory.Build.props          → Shared build settings across all projects
├── Directory.Packages.props       → Central Package Management (one place for all NuGet versions)
├── .editorconfig                  → Consistent code style
└── .github/workflows/build.yml   → CI pipeline
```

**Architecture style:** Layered Architecture with Separation of Concerns. The Api layer depends on Core, but Core knows nothing about the web layer. This follows the core principle of Clean Architecture (dependency flows inward) without the extra abstraction layers that would be over-engineering for 4 entities.

**Why not full Clean Architecture with 5+ projects?**
Clean Architecture (Domain, Application, Infrastructure, Presentation) shines in large systems with multiple modules and external integrations. For a clinic API with 4 entities, separating into 5+ projects adds complexity without proportional benefit. The current structure can evolve into full Clean Architecture if the project grows — for example, extracting an Infrastructure project to isolate EF Core from the Core layer.

### Error Handling Strategy — Two Layers

**Layer 1 — Result Pattern (expected business errors):**
Services return `Result<T>` instead of throwing exceptions. "Wrong password" or "duplicate email" are expected outcomes, not exceptional situations. The endpoint maps `Result.IsSuccess` to 200/201 and `Result.IsFailure` to 400/401/404.

**Layer 2 — Global Exception Handler (unexpected system errors):**
A middleware catches any unhandled exception (database crash, null reference, network timeout), logs the full details for developers, and returns a clean ProblemDetails JSON to the client. Stack traces are never exposed.

```text
User sends request
    → Validation (DTOs with data annotations)
        → Service returns Result<T> (business logic)
            → If unexpected error → Global Exception Handler
                → Client always gets clean JSON response
```

### Soft Delete — Why and Where

**Patient and Doctor use Soft Delete:**
Medical data should never be permanently deleted. When a patient or doctor is "deleted", EF Core Global Query Filter automatically hides them from all queries. The data stays in the database for audit trail and FK integrity.

**Appointments do NOT use Soft Delete:**
Appointments have a status lifecycle (Scheduled → Completed/Cancelled). Only Cancelled appointments can be hard deleted. The status system provides the audit trail, making soft delete redundant.

### Authentication Design

**Registration:** All new users register as Patient role by default. No self-assigned Admin — only an existing Admin can promote users via `PUT /api/users/{id}/role`.

**Login security:** Returns the same error message ("Invalid credentials") for both wrong password and non-existent email. This prevents email enumeration attacks.

**Single role per user:** Each user has exactly one role (Admin, Receptionist, or Patient). When Admin assigns a new role, the old role is removed. This is intentional for a clinic system — a person is either staff or patient, not both simultaneously.

## Entity Relationship Diagram

```text
ApplicationUser (ASP.NET Core Identity)
    │
    │ optional 1:1
    ▼
Patient ←──────────── Appointment ──────────────→ Doctor
  - Id                   - Id                      - Id
  - FullName             - PatientId (FK)           - FullName
  - Email (unique)       - DoctorId (FK)            - Email (unique)
  - Phone                - AppointmentDate          - Phone
  - DateOfBirth          - AppointmentTime          - Specialization
  - Gender               - DurationMinutes          - YearsOfExperience
  - Address              - Status                   - Bio
  - UserId (FK, opt.)    - Notes                    - IsAvailable
  - IsDeleted            - CreatedAt                - IsDeleted
  - DeletedAt            - UpdatedAt                - DeletedAt
  - CreatedAt                                       - CreatedAt
  - UpdatedAt                                       - UpdatedAt
```

**Key relationships:**

- Patient → ApplicationUser: Optional FK. Patients added by receptionist have no account (UserId = null). Patients who self-register have a linked account.
- Appointment → Patient: Required FK with Restrict delete. Soft-deleted patients retain their appointment history.
- Appointment → Doctor: Required FK with Restrict delete. Soft-deleted doctors retain their appointment history.

## Role Permissions Matrix

| Action                | Admin  | Receptionist | Patient   |
|-----------------------|:------:|:------------:|:---------:|
| Register / Login      | ✅     | ✅           | ✅        |
| View doctors (public) | ✅     | ✅           | ✅        |
| Manage patients (CRUD)| ✅     | ✅           | ❌        |
| Manage appointments   | ✅     | ✅           | ❌        |
| Manage doctors (CRUD) | ✅     | ❌           | ❌        |
| Delete patients (soft)| ✅     | ❌           | ❌        |
| Delete appointments   | ✅     | ❌           | ❌        |
| Assign roles          | ✅     | ❌           | ❌        |

## Business Rules — Appointment Engine

These are the core rules that make this project more than CRUD:

**Booking rules (CreateAsync):**

1. Patient must exist and not be soft-deleted
2. Doctor must exist, not be soft-deleted, and be available (IsAvailable = true)
3. Appointment date cannot be in the past
4. Patient cannot have an overlapping Scheduled appointment on the same date
5. Doctor cannot have an overlapping Scheduled appointment on the same date

**Overlap detection formula:**

```text
Two appointments overlap when:
  existingStart < newEnd  AND  newStart < existingEnd

Back-to-back is allowed:
  10:00-10:30 and 10:30-11:00 → NO overlap (10:30 < 10:30 is false)
```

**Update rules (UpdateAsync):**

- Only Scheduled appointments can be updated
- At least one field must be provided
- New date cannot be in the past
- Conflict check runs only when date/time/duration actually changed
- Current appointment is excluded from conflict check (no self-conflict)

**Status transition rules (UpdateStatusAsync):**

```text
Scheduled  → Completed  ✅
Scheduled  → Cancelled  ✅
Completed  → anything   ❌ (final state)
Cancelled  → anything   ❌ (final state)
```

**Delete rules (DeleteAsync):**

- Only Cancelled appointments can be deleted (hard delete)
- Scheduled/Completed appointments cannot be deleted (preserves medical history)
- Admin only

## Sprint Roadmap

| Sprint                   | Focus                        | Key Deliverables                                                                                         | Duration    |
|--------------------------|------------------------------|----------------------------------------------------------------------------------------------------------|-------------|
| **Sprint 1**             | Project Setup + CI           | Solution structure, CPM, Result Pattern, Error Handling, Health Check, GitHub Actions, Branch Protection | ~4.5 hours  |
| **Sprint 2**             | Authentication               | Identity, JWT, Refresh Token, Roles seeding, Input Validation, Logout                                    | ~6 hours    |
| **Sprint 3**             | Patients CRUD                | ISoftDeletable, Patient model, Soft Delete, Global Query Filter, Search, Pagination, Assign Role         | ~6.5 hours  |
| **Sprint 4**             | Doctors CRUD                 | Doctor model, Soft Delete, Public endpoints, Available doctors filter                                    | ~5 hours    |
| **Sprint 5**             | Appointments                 | Business rules, Overlap detection, Status transitions, Date/Status filters                               | ~7 hours    |
| **Sprint 6**             | Polish                       | Error consistency, Scalar UI, README, Code cleanup, Git strategy, Coverage review                        | ~4.5 hours  |
| **Total estimated time** |                              |                                                                                                          | ~33.5 hours |

## What This Project Demonstrates

**For interviewers evaluating backend skills:**

- Clean project structure with clear separation of concerns
- JWT authentication with refresh token rotation
- Role-based authorization with real permission boundaries
- Result Pattern separating business errors from system errors
- Soft Delete with EF Core Global Query Filters
- Real business logic: overlap detection, status state machine, audit trail preservation
- Input validation at DTO level with data annotations
- Comprehensive testing strategy (unit + integration)
- CI/CD pipeline with branch protection
- Professional documentation and consistent code style

**Engineering decisions that go beyond junior level:**

- `Directory.Build.props` for shared build settings (TreatWarningsAsErrors)
- `Directory.Packages.props` for Central Package Management
- `IgnoreQueryFilters()` for email uniqueness across soft-deleted records
- Filtered Unique Index (`HasFilter("IsDeleted = 0")`) — allows deleted + active records with same email
- `DeleteBehavior.Restrict` on FK relationships to protect data integrity
- Same error message on login failure to prevent email enumeration
- Default Patient role on registration to prevent privilege escalation
- Configurable token expiry via appsettings (not hardcoded)
- Health Check endpoint for production monitoring readiness
