# Clinic Management API

## Project Identity

A RESTful API for managing clinic appointments, patients, and doctors. Portfolio project demonstrating backend engineering with real business logic — not just CRUD.

## Tech Stack

- .NET 10 / C# 14 — Minimal API (not Controllers)
- Entity Framework Core 10 + SQL Server (SQL Server Express locally)
- ASP.NET Core Identity + JWT + Refresh Tokens
- xUnit (Unit + Integration tests with InMemory DB)
- GitHub Actions CI on push to main/develop
- Central Package Management (Directory.Packages.props)
- Solution format: `.slnx`

## Solution Structure

```markdown
ClinicManagementAPI.slnx
├── ClinicManagementAPI.Api/          → Web layer (depends on Core)
│   ├── Endpoints/                    → Static classes with MapXxxEndpoints()
│   ├── Filters/                      → ValidationFilter<T> (Data Annotations)
│   ├── Middleware/                    → GlobalExceptionHandler (IExceptionHandler)
│   └── Program.cs
├── ClinicManagementAPI.Core/         → Business layer (knows nothing about Web)
│   ├── Data/                         → AppDbContext, DatabaseSeeder
│   ├── DTOs/                         → All DTOs live here (Auth/, Patients/, Doctors/, Appointments/)
│   ├── Interfaces/                   → IAuthService, IPatientService, IDoctorService, IAppointmentService, ISoftDeletable
│   ├── Models/                       → Entities, enums, Result<T>, JwtSettings, AppRoles, XxxMappings
│   ├── Services/                     → AuthService, PatientService, DoctorService, AppointmentService
│   └── Migrations/
├── ClinicManagementAPI.Tests/
│   ├── Unit/                         → XxxServiceTests.cs (InMemory DB per test)
│   └── Integration/                  → XxxEndpointsTests.cs + CustomWebApplicationFactory
├── Directory.Build.props             → net10.0, Nullable, TreatWarningsAsErrors, EnforceCodeStyleInBuild
├── Directory.Packages.props          → CPM with MicrosoftExtensionsVersion property
└── Roadmap/                          → Sprint checklists (Sprint-1 through Sprint-6)
```

## Current State

All 6 sprints are complete. Project is on `develop` branch, pending final PR to `main`.

- Sprint 1: Project setup, CI, Result pattern, GlobalExceptionHandler, Health Check
- Sprint 2: Identity, JWT, Refresh Token rotation, Roles seeding, Auth endpoints + tests
- Sprint 3: Patient CRUD, ISoftDeletable, Soft Delete, Global Query Filter, Pagination, Search, Assign Role + tests
- Sprint 4: Doctors CRUD (same pattern as Patients, public GET endpoints) + tests
- Sprint 5: Appointments + Business Logic (overlap detection, status transitions) + tests
- Sprint 6: Polish, Scalar UI, README, coverage review, branching strategy

Total: 181 tests (Unit + Integration). Coverage: AppointmentService 95.2%, AuthService 98.1%, PatientService 99.1%, DoctorService 100%.

## Commands

```bash
# Build
dotnet build

# Run
dotnet run --project ClinicManagementAPI.Api

# Test
dotnet test --verbosity normal

# Test with coverage
dotnet test --collect:"XPlat Code Coverage"

# Add migration
dotnet ef migrations add <Name> --project ClinicManagementAPI.Core --startup-project ClinicManagementAPI.Api

# Apply migration
dotnet ef database update --project ClinicManagementAPI.Core --startup-project ClinicManagementAPI.Api

# User secrets (connection string)
dotnet user-secrets set "ConnectionStrings:ClinicDb" "Server=localhost\SQLEXPRESS;Database=ClinicDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ClinicManagementAPI.Api

# User secrets (JWT key)
dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-characters" --project ClinicManagementAPI.Api
```

## Architecture Decisions — Follow These Strictly

### Error Handling: Two Layers

- **Result\<T\> pattern** for expected business errors. Services return `Result<T>.Success(value)` or `Result<T>.Failure("message", statusCode)`. Never throw exceptions for business logic.
- **GlobalExceptionHandler** (IExceptionHandler) catches unexpected errors (DB crash, null ref). Returns ProblemDetails JSON. Stack traces never exposed.

### No Repository Pattern

Use `AppDbContext` directly in services. EF Core IS the repository + unit of work. No `IRepository<T>` abstraction — it adds indirection without value for this project.

### Validation: Data Annotations + ValidationFilter\<T\>

Use `[Required]`, `[EmailAddress]`, `[Range]`, `[MinLength]`, `[MaxLength]` on DTO properties. Endpoints apply `.AddEndpointFilter<ValidationFilter<T>>()`. Never use FluentValidation.

### Mapping: Manual Extension Methods

Create `XxxMappings.cs` next to the model in `Core/Models/`. Static extension methods like `patient.ToResponse()`. Never use AutoMapper.

### Soft Delete: ISoftDeletable + Global Query Filter

Patient and Doctor implement `ISoftDeletable` (IsDeleted + DeletedAt). AppDbContext adds `.HasQueryFilter(x => !x.IsDeleted)`. Use `.IgnoreQueryFilters()` only for email uniqueness checks. Appointments do NOT use soft delete — they use Status lifecycle.

### DTOs Live in Core/DTOs/

Not in Api/DTOs/. Grouped by feature: `Core/DTOs/Auth/`, `Core/DTOs/Patients/`, `Core/DTOs/Doctors/`, `Core/DTOs/Appointments/`. Shared DTOs at `Core/DTOs/` root (PaginationRequest, PagedResponse).

### DI Registration in Program.cs

Register services as `AddScoped<IXxxService, XxxService>()`. Group registrations logically: Database → Identity → Authentication → Authorization → Application Services → Health Checks.

### Endpoint Organization

Each entity has a static class with `MapXxxEndpoints(this WebApplication app)` extension method in `Api/Endpoints/`. Use `MapGroup("/api/xxx")` for grouping. Private static async methods for each handler.

### Database

- AppDbContext inherits `IdentityDbContext<ApplicationUser>` (primary constructor syntax)
- Filtered Unique Index on Email: `.HasIndex(x => x.Email).IsUnique().HasFilter("IsDeleted = 0")`
- FK relationships with `DeleteBehavior.Restrict` (never Cascade)
- Global Query Filters for soft-deletable entities

## Naming Conventions

| Element            | Convention                       | Example                                               |
|--------------------|----------------------------------|-------------------------------------------------------|
| DTOs (request)     | {Action}{Entity}Request          | CreatePatientRequest                                  |
| DTOs (response)    | {Entity}Response                 | PatientResponse                                       |
| Service interfaces | I{Entity}Service                 | IPatientService                                       |
| Services           | {Entity}Service                  | PatientService                                        |
| Endpoints          | {Entity}Endpoints                | PatientEndpoints                                      |
| Unit tests         | {Service}Tests                   | PatientServiceTests                                   |
| Integration tests  | {Entity}EndpointsTests           | PatientEndpointsTests                                 |
| Test methods       | {Method}\_{Scenario}\_{Expected} | CreateAsync\_WithDuplicateEmail\_ReturnsFailureResult |
| Async methods      | PascalCase + Async suffix        | GetByIdAsync                                          |
| Private fields     | \_camelCase                      | \_context, \_userManager                              |

## Code Style Rules

- File-scoped namespaces always: `namespace X;`
- `var` when type is obvious from right side; explicit type when not
- Pattern matching: `is null`, `is not null` (never `== null`)
- Collection expressions: `string[] roles = ["Admin", "Receptionist"]`
- String interpolation: `$"Patient {name} not found"`
- One class per file (file name = class name)
- Comments explain WHY, not WHAT
- Conventional commits: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`
- File member ordering: private fields → constructor → public methods (interface order) → private helpers
- Using directive ordering: `System.*` → `Microsoft.*` → `ClinicManagementAPI.*` (blank line between groups, alphabetized within)

## Patterns for Adding New Entities

Follow the same pattern used for Patient, Doctor, and Appointment:

1. Model in `Core/Models/` (implement ISoftDeletable if soft delete needed)
2. DTOs in `Core/DTOs/{Entity}/` (Create, Update, Response)
3. Interface in `Core/Interfaces/`
4. Service in `Core/Services/`
5. Mapping extensions in `Core/Models/{Entity}Mappings.cs`
6. Configure entity in `AppDbContext.OnModelCreating`
7. Add DbSet to AppDbContext
8. Migration
9. Endpoints in `Api/Endpoints/`
10. Register service in Program.cs
11. Map endpoints in Program.cs
12. Unit tests in `Tests/Unit/`
13. Integration tests in `Tests/Integration/`

## Anti-Patterns — Never Do These

- Never use `DateTime.Now` — use `DateTime.UtcNow`
- Never throw exceptions for business errors — use `Result<T>.Failure()`
- Never expose raw models to API — always use DTOs
- Never include IsDeleted/DeletedAt in response DTOs
- Never wrap EF Core in a Repository pattern
- Never use AutoMapper or FluentValidation
- Never hardcode connection strings or JWT keys (use User Secrets)
- Never use `new HttpClient()` — use `IHttpClientFactory` if needed
- Never skip CancellationToken propagation in async methods
- Never use `.Result` or `.Wait()` on async calls (deadlock risk)
- Never commit appsettings.Development.json or coverage reports

## Testing Conventions

- Unit tests: InMemory DB with `Guid.NewGuid()` per test for isolation
- Integration tests: `CustomWebApplicationFactory` with `IClassFixture<>`, shared InMemory DB
- Roles must be seeded manually in unit tests (no Program.cs runs)
- Test naming: `MethodName_Scenario_ExpectedResult`
- Structure: Arrange-Act-Assert with blank lines between sections
- Target: 70%+ coverage per service, 80%+ for AppointmentService
- Both Unit + Integration tests for every feature

## Roles and Authorization

Roles: Admin, Doctor, Receptionist, Patient (seeded on startup via DatabaseSeeder).

- Public endpoints (no auth): GET /api/doctors, GET /api/doctors/available, GET /api/doctors/{id}
- Admin + Receptionist: Patient CRUD, Appointment CRUD
- Admin only: Doctor CRUD write operations, Delete patients, Delete appointments, Assign roles
- All new registrations default to Patient role

## Business Rules (Sprint 5 — Appointments)

- Appointments cannot be booked in the past
- Patient and Doctor must exist and not be soft-deleted
- Doctor must have IsAvailable = true
- Overlap formula: `existingStart < newEnd AND newStart < existingEnd`
- Back-to-back is allowed (10:30 < 10:30 is false)
- Only Scheduled appointments can be updated
- Status transitions: Scheduled → Completed ✅ | Scheduled → Cancelled ✅ | others ❌
- Only Cancelled appointments can be hard deleted
- Update conflict check excludes current appointment (no self-conflict)
