# Sprint 6 — Polish + Documentation + Coverage Review

**Project:** Clinic Management API  
**Sprint Duration:** 1 Week  
**Expected Time:** ~4.5 hours  
**Goal:** Make the project interview-ready — clean, documented, and professional  
**Prerequisites:** All previous sprints are complete, all tests are passing, CI is green

---

## Section 1 — Error Response Consistency Review

**Expected Time: 20 minutes**  
**Goal:** Verify that ALL endpoints return consistent ProblemDetails format (Global Error Handler was created in Sprint 1)

```markdown
[ ] Verify GlobalExceptionHandler (created in Sprint 1) is working correctly

[ ] Test all error scenarios return consistent ProblemDetails format via Scalar UI:
    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Bad Request",
      "status": 400,
      "detail": "Email already registered"
    }

    Test these scenarios:
    - 400 → Send invalid data (missing required field)
    - 401 → Call protected endpoint without token
    - 403 → Call Admin endpoint with Receptionist token
    - 404 → Request non-existent patient/doctor/appointment
    - 500 → Temporarily throw exception in a test endpoint

[ ] Fix any endpoint that returns a non-ProblemDetails error format
    All Result.Failure responses should map to Results.Problem()
```

---

## Section 2 — OpenAPI + Scalar UI

**Expected Time: 30 minutes**  
**Goal:** Professional API documentation that any developer can use immediately

```markdown
[ ] Verify Microsoft.AspNetCore.OpenApi is installed (already in Directory.Packages.props)

[ ] Configure OpenAPI in Program.cs
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Info.Title   = "Clinic Management API";
            document.Info.Version = "v1";
            document.Info.Description = "A RESTful API for managing clinic appointments, patients, and doctors.";
            return Task.CompletedTask;
        });
    });

[ ] Add JWT authentication scheme to OpenAPI
    So Scalar UI shows a "Bearer Token" input field for protected endpoints

[ ] Configure Scalar UI in Program.cs
    app.MapScalarApiReference(options =>
    {
        options.Title = "Clinic Management API";
        options.Theme = ScalarTheme.Purple;
    });

[ ] Verify Scalar UI shows all endpoints grouped correctly:
    ✅ Auth endpoints (register, login, refresh, logout)
    ✅ Patient endpoints (with search + pagination parameters)
    ✅ Doctor endpoints (with search + pagination parameters)
    ✅ Appointment endpoints (with search + date filter + status filter)
    ✅ Protected endpoints show lock icon
    ✅ Public endpoints (GET doctors) show no lock icon
```

---

## Section 3 — README Update

**Expected Time: 45 minutes**  
**Goal:** First thing an interviewer sees — make it count

```markdown
[ ] Update README.md with final content:

    # Clinic Management API

    ![CI](https://github.com/<username>/clinic-management-api/actions/workflows/build.yml/badge.svg)

    A production-ready RESTful API built with ASP.NET Core Minimal API
    for managing clinic appointments, patients, and doctors.

    ## Tech Stack
    - .NET 10 / ASP.NET Core Minimal API
    - Entity Framework Core + SQL Server
    - ASP.NET Core Identity + JWT Authentication
    - Refresh Token with secure revocation
    - Result Pattern for business error handling
    - xUnit + GitHub Actions CI
    - OpenAPI + Scalar UI

    ## Project Structure
    ClinicManagementAPI/
    ├── ClinicManagementAPI.Api/       → Endpoints, DTOs, Middleware, Program.cs
    ├── ClinicManagementAPI.Core/      → Models, Services, Interfaces, Data
    ├── ClinicManagementAPI.Tests/     → Unit + Integration Tests
    ├── Directory.Build.props          → Shared build settings
    ├── Directory.Packages.props       → Central Package Management
    └── .editorconfig                  → Code style rules

    ## Architecture Decisions
    - 3-project Clean Architecture (Api → Core ← Tests)
    - Result Pattern for expected errors (no exceptions for business logic)
    - Global Error Handler for unexpected errors (DB crash, null ref)
    - Central Package Management for consistent NuGet versions
    - User Secrets for sensitive configuration (never committed)

    ## Role Permissions

    | Action                  | Admin | Receptionist | Patient |
    |-------------------------|:-----:|:------------:|:-------:|
    | Register / Login        |  ✅   |     ✅       |   ✅    |
    | View doctors (public)   |  ✅   |     ✅       |   ✅    |
    | Manage patients (CRUD)  |  ✅   |     ✅       |   ❌    |
    | Manage appointments     |  ✅   |     ✅       |   ❌    |
    | Manage doctors (CRUD)   |  ✅   |     ❌       |   ❌    |
    | Delete patients         |  ✅   |     ❌       |   ❌    |
    | Delete appointments     |  ✅   |     ❌       |   ❌    |
    | Assign roles            |  ✅   |     ❌       |   ❌    |

    ## API Endpoints

    ### Auth
    | Method | Endpoint               | Access   | Description              |
    |--------|------------------------|----------|--------------------------|
    | POST   | /api/auth/register     | Public   | Register new user        |
    | POST   | /api/auth/login        | Public   | Login and get JWT        |
    | POST   | /api/auth/refresh      | Public   | Refresh JWT token        |
    | POST   | /api/auth/logout       | JWT      | Revoke refresh token     |
    | PUT    | /api/users/{id}/role   | Admin    | Assign role to user      |

    ### Patients
    | Method | Endpoint               | Access           | Description          |
    |--------|------------------------|------------------|----------------------|
    | GET    | /api/patients          | Admin, Recep.    | List + search + page |
    | GET    | /api/patients/{id}     | Admin, Recep.    | Get by ID            |
    | POST   | /api/patients          | Admin, Recep.    | Create patient       |
    | PUT    | /api/patients/{id}     | Admin, Recep.    | Partial update       |
    | DELETE | /api/patients/{id}     | Admin            | Delete patient       |

    ### Doctors
    | Method | Endpoint                 | Access   | Description              |
    |--------|--------------------------|----------|--------------------------|
    | GET    | /api/doctors             | Public   | List + search + page     |
    | GET    | /api/doctors/available   | Public   | Available doctors only   |
    | GET    | /api/doctors/{id}        | Public   | Get by ID                |
    | POST   | /api/doctors             | Admin    | Create doctor            |
    | PUT    | /api/doctors/{id}        | Admin    | Partial update           |
    | DELETE | /api/doctors/{id}        | Admin    | Delete doctor            |

    ### Appointments
    | Method | Endpoint                          | Access        | Description             |
    |--------|-----------------------------------|---------------|-------------------------|
    | GET    | /api/appointments                 | Admin, Recep. | List + filter + page    |
    | GET    | /api/appointments/{id}            | Admin, Recep. | Get by ID               |
    | GET    | /api/appointments/patient/{id}    | Admin, Recep. | By patient              |
    | GET    | /api/appointments/doctor/{id}     | Admin, Recep. | By doctor               |
    | POST   | /api/appointments                 | Admin, Recep. | Book appointment        |
    | PUT    | /api/appointments/{id}            | Admin, Recep. | Reschedule              |
    | PATCH  | /api/appointments/{id}/status     | Admin, Recep. | Complete or cancel      |
    | DELETE | /api/appointments/{id}            | Admin         | Delete (cancelled only) |

    ## Business Rules
    - Appointments cannot be booked in the past
    - Patients cannot have overlapping appointments
    - Doctors cannot have overlapping appointments
    - Only available doctors can receive new appointments
    - Only scheduled appointments can be updated or rescheduled
    - Completed appointments cannot be changed
    - Cancelled appointments cannot be changed
    - Only cancelled appointments can be deleted (audit trail preserved)
    - Status transitions: Scheduled → Completed ✅ | Scheduled → Cancelled ✅ | others ❌

    ## How to Run
    ```bash
    # 1. Clone the repository
    git clone https://github.com/<username>/clinic-management-api.git
    cd clinic-management-api

    # 2. Configure User Secrets
    dotnet user-secrets set "ConnectionStrings:ClinicDb" "Server=localhost;Database=ClinicDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ClinicManagementAPI.Api
    dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-characters" --project ClinicManagementAPI.Api

    # 3. Apply migrations
    dotnet ef database update --project ClinicManagementAPI.Core --startup-project ClinicManagementAPI.Api

    # 4. Run the API
    dotnet run --project ClinicManagementAPI.Api

    # 5. Open Scalar UI
    # Navigate to: https://localhost:<port>/scalar/v1
    ```

    ## Running Tests
    ```bash
    # Run all tests
    dotnet test --verbosity normal

    # Run with coverage
    dotnet test --collect:"XPlat Code Coverage"
    ```
```

---

## Section 4 — Code Cleanup

**Expected Time: 30 minutes**  
**Goal:** Clean, readable code that an interviewer can open and understand immediately

```markdown
[ ] Remove all unused using statements across all files
    Windsurf: Ctrl + Shift + P → "Remove Unused Usings"

[ ] Verify all files follow consistent naming conventions
    - Classes: PascalCase
    - Methods: PascalCase
    - Variables: camelCase
    - Constants: PascalCase

[ ] Verify all English comments are clear and meaningful
    Bad:  // get patient
    Good: // Return 404 if patient does not exist

[ ] Verify Program.cs is organized in logical sections
    Section 1: Builder services (DbContext, Identity, JWT, Services, Health Checks)
    Section 2: Middleware pipeline (UseExceptionHandler, UseAuthentication, UseAuthorization)
    Section 3: Endpoint mapping (MapAuthEndpoints, MapPatientEndpoints, MapDoctorEndpoints, MapAppointmentEndpoints, MapHealthChecks)

[ ] Verify folder structure is clean with no orphan files
    Run: dotnet build → should have 0 warnings (TreatWarningsAsErrors from Sprint 1)
```

---

## Section 5 — Git History + Branching Strategy

**Expected Time: 30 minutes**  
**Goal:** Clean commit history and branching that shows professional development workflow

```markdown
[ ] Verify all commits follow conventional commits format
    Examples:
    feat: add patient registration endpoint
    fix: handle duplicate email in doctor creation
    test: add appointment business rule tests
    docs: update README with API endpoints
    refactor: extract overlap detection into private method
    chore: update NuGet packages

[ ] Verify branching strategy is clean
    - main branch → production-ready code, protected by branch rules
    - develop branch → active development, all sprints merged here first
    - Feature branches (optional) → sprint-1/setup, sprint-2/auth, etc.
    - All merges to main should be via Pull Request

    ⚠️ If you've been pushing directly to main until now:
    1. Create develop branch from main: git checkout -b develop
    2. From now on, work on develop and PR to main
    3. This shows the interviewer you understand Git workflow

[ ] Verify .gitignore is complete
    Must include:
    ✅ **/bin/
    ✅ **/obj/
    ✅ **/appsettings.Development.json
    ✅ *.user
    ✅ .vs/

[ ] Verify no sensitive data was ever committed
    Command: git log --all --full-history -- "**/appsettings*"
    Should show no connection strings or JWT keys

[ ] Add LICENSE file (MIT) in the repo root
    Command: Create LICENSE file with MIT license text
    Why: Shows you understand open source practices
    Include your name and current year
```

---

## Section 6 — Coverage Review

**Expected Time: 30 minutes**  
**Goal:** Verify 70%+ coverage and identify any critical gaps

```markdown
[ ] Generate coverage report
    Command: dotnet test --collect:"XPlat Code Coverage"

[ ] Install coverage report tool (if not installed)
    Command: dotnet tool install -g dotnet-reportgenerator-globaltool

[ ] Generate HTML report
    Command: reportgenerator -reports:"**/coverage.cobertura.xml"
                             -targetdir:"coverage-report"
                             -reporttypes:Html

[ ] Open coverage report and verify:
    ✅ AppointmentService coverage → must be 80%+ (most critical — has business rules)
    ✅ AuthService coverage        → must be 75%+
    ✅ PatientService coverage     → must be 70%+
    ✅ DoctorService coverage      → must be 70%+

[ ] Write missing tests for any critical uncovered logic
    Priority: Business rules in AppointmentService > Auth logic > CRUD logic

[ ] Add coverage-report/ to .gitignore (generated files, not committed)
```

---

## Section 7 — Final CI Verification

**Expected Time: 20 minutes**  
**Goal:** CI pipeline is fully green across all sprints before marking project as complete

```markdown
[ ] Push final changes to GitHub (via PR from develop to main)

[ ] Verify GitHub Actions shows:
    ✅ Build passes
    ✅ All Auth tests pass
    ✅ All Patient tests pass
    ✅ All Doctor tests pass
    ✅ All Appointment tests pass
    ✅ Coverage is 70%+
    ✅ 0 build warnings

[ ] Verify Branch Protection is still active on main

[ ] Verify GitHub Actions badge shows on README.md
    Badge should show "passing" in green

[ ] Final manual test via Scalar UI:
    1. Register a new user → get JWT
    2. Login → get JWT + Refresh Token
    3. Create a patient → verify 201
    4. Create a doctor → verify 201
    5. Book an appointment → verify 201 + business rules
    6. Try booking overlapping appointment → verify 400
    7. Complete the appointment → verify status change
    8. Try changing completed appointment → verify 400
    9. Refresh token → verify new JWT
    10. Logout → verify token revoked
```

---

## Section 8 — Diagrams

**Expected Time: 30 minutes**  
**Goal:** Create and update necessary system diagrams for Sprint 6 features (Polish, Documentation)

```markdown
[ ] Review docs/ to determine required diagrams for Sprint 6
[ ] Update/Create Component/Sequence Diagrams for final polish and architecture
[ ] Verify PlantUML/Markdown diagrams render correctly
```

---

## Sprint 6 — Done Definition

```markdown
✅ All error responses use consistent ProblemDetails format
✅ Scalar UI shows all endpoints with JWT authentication support
✅ README.md includes: project structure, role permissions table, endpoint tables, business rules, run commands
✅ GitHub Actions badge is visible on README
✅ Code is clean with 0 build warnings
✅ All commits follow conventional commits format
✅ Branching strategy is documented (develop → PR → main)
✅ LICENSE file (MIT) is in the repo root
✅ No sensitive data in git history
✅ coverage-report/ is in .gitignore
✅ Coverage is 70%+ across all services
✅ AppointmentService coverage is 80%+
✅ CI pipeline is fully green on GitHub
✅ Final manual test via Scalar UI passes all 10 steps
✅ Diagrams for Sprint 6 are created and updated
```

---

## Project Complete 🎉

```markdown
Sprint 1 ✅ → Project Setup + CI + Result Pattern + Error Handling + Health Check
Sprint 2 ✅ → Authentication + JWT + Refresh Token + Input Validation
Sprint 3 ✅ → Patients CRUD + Role Authorization + Assign Role + Search
Sprint 4 ✅ → Doctors CRUD + Public Endpoints + Search
Sprint 5 ✅ → Appointments + Business Logic + Overlap Detection + Filters
Sprint 6 ✅ → Polish + Documentation + Coverage Review + Branching Strategy
```

**You now have a portfolio project that demonstrates:**
- Clean Architecture (3-project solution)
- Central Package Management + Shared Build Settings
- JWT Authentication with Refresh Token
- Role-based Authorization (Admin, Receptionist, Patient)
- Result Pattern for expected error handling
- Global Error Handler for unexpected errors
- Real Business Logic (overlap detection, status transitions, audit trail)
- Search, Filtering, and Pagination
- 70%+ test coverage (Unit + Integration)
- CI/CD pipeline with Branch Protection
- Professional documentation (README + Scalar UI)
- Clean Git history with conventional commits
