# Development Standards — Clinic Management API

> This document defines how code is written in this project.  
> Machine-enforceable rules live in `.editorconfig` and `Directory.Build.props`.  
> This document covers the **why** behind those rules + standards that only humans can enforce.

---

## 1 — Naming Conventions (Microsoft C# Guidelines)

### 1.1 — General Rules

| Element                   | Convention                     | Example                                        |
|---------------------------|--------------------------------|------------------------------------------------|
| Classes, Records, Structs | PascalCase                     | `PatientService`, `AuthResponse`               |
| Interfaces                | IPascalCase (I prefix)         | `IPatientService`, `ISoftDeletable`            |
| Methods                   | PascalCase                     | `GetByIdAsync`, `CreateAsync`                  |
| Properties                | PascalCase                     | `FullName`, `IsDeleted`                        |
| Public constants          | PascalCase                     | `AppRoles.Admin`                               |
| Private fields            | _camelCase (underscore prefix) | `_userManager`, `_context`                     |
| Local variables           | camelCase                      | `patient`, `totalCount`                        |
| Parameters                | camelCase                      | `request`, `patientId`                         |
| Enums                     | PascalCase (singular)          | `Gender.Male`, `AppointmentStatus.Scheduled`   |
| Async methods             | PascalCase + Async suffix      | `RegisterAsync`, `DeleteAsync`                 |
| Generic type parameters   | T prefix + PascalCase          | `Result<T>`, `PagedResponse<T>`                |

### 1.2 — Naming Patterns Specific to This Project

| Element                  | Pattern                          | Example                                               |
|--------------------------|----------------------------------|-------------------------------------------------------|
| DTOs (request)           | {Action}{Entity}Request          | `CreatePatientRequest`, `LoginRequest`                |
| DTOs (response)          | {Entity}Response                 | `PatientResponse`, `AuthResponse`                     |
| Service interfaces       | I{Entity}Service                 | `IPatientService`, `IAuthService`                     |
| Service implementations  | {Entity}Service                  | `PatientService`, `AuthService`                       |
| Endpoint files           | {Entity}Endpoints                | `AuthEndpoints.cs`, `PatientEndpoints.cs`             |
| Unit test classes        | {Class}Tests                     | `PatientServiceTests`, `AuthServiceTests`             |
| Integration test classes | {Entity}EndpointsTests           | `PatientEndpointsTests`                               |
| Test methods             | {Method}\_{Scenario}\_{Expected} | `CreateAsync_WithDuplicateEmail_ReturnsFailureResult` |

### 1.3 — Things to Avoid

- Never use Hungarian notation: ~~`strName`~~, ~~`intAge`~~, ~~`boolIsActive`~~
- Never use abbreviations unless universally known: ~~`pt`~~ → `patient`, ~~`appt`~~ → `appointment`. Exceptions: `Id`, `Dto`, `Db`
- Never use single-letter variables except in lambdas: `p => p.Email` is fine, `var p = GetPatient()` is not
- Never prefix class names with C: ~~`CPatient`~~ (that's C++ style)

---

## 2 — Code Style

### 2.1 — File-Scoped Namespaces (Always)

```csharp
// ✅ Correct — file-scoped namespace (C# 10+)
namespace ClinicManagementAPI.Core.Models;

public class Patient : ISoftDeletable
{
    // entire file is in this namespace
}

// ❌ Wrong — block-scoped namespace (old style)
namespace ClinicManagementAPI.Core.Models
{
    public class Patient : ISoftDeletable
    {
        // unnecessary indentation
    }
}
```

**Why:** Saves one indentation level in every file. Modern C# standard since .NET 6.

### 2.2 — `var` Usage

```csharp
// ✅ Use var when the type is obvious from the right side
var patient = new Patient();
var patients = await _context.Patients.ToListAsync();
var result = Result<AuthResponse>.Success(response);

// ❌ Don't use var when the type is not obvious
var data = GetResult();       // What type is this?
var x = ProcessRequest();     // Completely unclear

// ✅ Use explicit type when var would be unclear
PatientResponse response = MapToResponse(patient);
int totalCount = await query.CountAsync();
```

**Rule:** Use `var` when the type is visible on the same line. Use explicit type when it's not.

### 2.3 — Expression Body vs Block Body

```csharp
// ✅ Use expression body for simple one-liners
public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

// ✅ Use block body for anything with logic
public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user is null)
        return Result<AuthResponse>.Failure("Invalid credentials", 401);
    
    // ... more logic
}
```

### 2.4 — Pattern Matching (Prefer Modern Syntax)

```csharp
// ✅ Modern pattern matching
if (user is null)
    return Result.Failure("Not found", 404);

if (patient is not null)
    return Result.Success(patient);

// ❌ Old style null checks
if (user == null)    // works but less readable
if (patient != null) // works but less readable
```

### 2.5 — String Handling

```csharp
// ✅ Use string interpolation
var message = $"Patient {patient.FullName} not found";

// ✅ Use raw string literals for multi-line (C# 11+)
var json = """
    {
        "status": 500,
        "title": "Internal Server Error"
    }
    """;

// ❌ Don't use string.Format or concatenation
var message = string.Format("Patient {0} not found", name);  // old style
var message = "Patient " + name + " not found";               // messy
```

### 2.6 — Async/Await Conventions

```csharp
// ✅ Always use Async suffix
public async Task<Result<PatientResponse>> GetByIdAsync(int id)

// ✅ Always await async calls (no fire-and-forget)
var patient = await _context.Patients.FindAsync(id);

// ✅ Use ConfigureAwait(false) in library code (Core project)
var patient = await _context.Patients.FindAsync(id).ConfigureAwait(false);

// ❌ Never use .Result or .Wait() — causes deadlocks
var patient = _context.Patients.FindAsync(id).Result;  // DEADLOCK RISK
```

### 2.7 — Collection Expressions (C# 12+)

```csharp
// ✅ Modern collection expressions
string[] roles = ["Admin", "Receptionist", "Patient"];
List<string> errors = [error1, error2];

// ❌ Old style
var roles = new string[] { "Admin", "Receptionist", "Patient" };
var errors = new List<string> { error1, error2 };
```

---

## 3 — Architecture Patterns

### 3.1 — Result Pattern Usage

```csharp
// ✅ Services always return Result<T>
public async Task<Result<PatientResponse>> GetByIdAsync(int id)
{
    var patient = await _context.Patients.FindAsync(id);
    if (patient is null)
        return Result<PatientResponse>.Failure("Patient not found", 404);
    
    return Result<PatientResponse>.Success(patient.ToResponse());
}

// ✅ Endpoints map Result to HTTP response
app.MapGet("/api/patients/{id}", async (int id, IPatientService service) =>
{
    var result = await service.GetByIdAsync(id);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Problem(result.Error, statusCode: result.StatusCode);
});

// ❌ Never throw exceptions for business errors
throw new NotFoundException("Patient not found");  // Wrong — use Result.Failure
```

**Rule:** `Result.Failure` for expected errors. Exceptions only for unexpected errors (DB crash, null ref).

### 3.2 — Mapping Convention (Manual Extension Methods)

```csharp
// ✅ Create static extension methods in a Mappings file per entity
// Location: Core/Models/PatientMappings.cs
public static class PatientMappings
{
    public static PatientResponse ToResponse(this Patient patient)
    {
        return new PatientResponse
        {
            Id = patient.Id,
            FullName = patient.FullName,
            Email = patient.Email,
            Phone = patient.Phone,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender.ToString(),
            Address = patient.Address,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };
    }
}

// Usage in service:
return Result<PatientResponse>.Success(patient.ToResponse());
```

**Why manual over AutoMapper:** Compile-time safe, debuggable, no magic. Each mapping is explicit and visible.

### 3.3 — Dependency Injection Registration

```csharp
// ✅ Register services in logical groups in Program.cs
// --- Database ---
builder.Services.AddDbContext<AppDbContext>(options => ...);

// --- Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>() ...

// --- Authentication ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) ...

// --- Application Services ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// --- Health Checks ---
builder.Services.AddHealthChecks() ...
```

### 3.4 — Endpoint Organization

```csharp
// ✅ Each entity has its own static class with MapXxxEndpoints extension method
// Location: Api/Endpoints/PatientEndpoints.cs
public static class PatientEndpoints
{
    public static void MapPatientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/patients")
                       .RequireAuthorization();

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id}", UpdateAsync);
        group.MapDelete("/{id}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync(...) { ... }
    private static async Task<IResult> GetByIdAsync(...) { ... }
    // ...
}

// In Program.cs:
app.MapAuthEndpoints();
app.MapPatientEndpoints();
app.MapDoctorEndpoints();
app.MapAppointmentEndpoints();
```

---

## 4 — File & Folder Organization

### 4.1 — One Class Per File (Always)

```markdown
✅ Patient.cs         → contains only Patient class
✅ PatientService.cs  → contains only PatientService class
✅ Gender.cs          → contains only Gender enum

❌ Models.cs          → contains Patient, Doctor, Gender all in one file
```

**Exception:** Small related types can share a file. Example: `Result.cs` can contain both `Result<T>` class and `IResult` interface if they're tightly coupled.

### 4.2 — File Naming = Class Naming

```markdown
Class name: PatientService     → File name: PatientService.cs     ✅
Class name: ISoftDeletable     → File name: ISoftDeletable.cs     ✅
Class name: CreatePatientRequest → File name: CreatePatientRequest.cs ✅
```

### 4.3 — Folder Structure Rules

```markdown
Api/
├── DTOs/
│   ├── Auth/                  → Group DTOs by feature
│   │   ├── RegisterRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── AuthResponse.cs
│   ├── Patients/
│   │   ├── CreatePatientRequest.cs
│   │   ├── UpdatePatientRequest.cs
│   │   └── PatientResponse.cs
│   ├── PaginationRequest.cs   → Shared DTOs at root level
│   └── PagedResponse.cs
├── Endpoints/
│   ├── AuthEndpoints.cs       → One file per entity
│   ├── PatientEndpoints.cs
│   ├── DoctorEndpoints.cs
│   └── AppointmentEndpoints.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs
└── Program.cs

Core/
├── Data/
│   ├── AppDbContext.cs
│   └── DatabaseSeeder.cs
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IPatientService.cs
│   ├── IDoctorService.cs
│   ├── IAppointmentService.cs
│   └── ISoftDeletable.cs
├── Models/
│   ├── ApplicationUser.cs
│   ├── Patient.cs
│   ├── PatientMappings.cs     → Mapping extensions next to model
│   ├── Doctor.cs
│   ├── DoctorMappings.cs
│   ├── Appointment.cs
│   ├── AppointmentMappings.cs
│   ├── RefreshToken.cs
│   ├── JwtSettings.cs
│   ├── AppRoles.cs
│   ├── Gender.cs
│   ├── AppointmentStatus.cs
│   └── Result.cs
└── Services/
    ├── AuthService.cs
    ├── PatientService.cs
    ├── DoctorService.cs
    └── AppointmentService.cs

Tests/
├── Unit/
│   ├── AuthServiceTests.cs
│   ├── PatientServiceTests.cs
│   ├── DoctorServiceTests.cs
│   └── AppointmentServiceTests.cs
└── Integration/
    ├── AuthEndpointsTests.cs
    ├── PatientEndpointsTests.cs
    ├── DoctorEndpointsTests.cs
    └── AppointmentEndpointsTests.cs
```

---

## 5 — Comments & Documentation

### 5.1 — When to Comment

```csharp
// ✅ Comment WHY, not WHAT
// Exclude current appointment from conflict check to avoid self-conflict
var hasConflict = await _context.Appointments
    .Where(a => a.Id != appointmentId && a.DoctorId == doctorId)
    .AnyAsync(a => a.AppointmentTime < newEnd && newStart < a.EndTime);

// ❌ Don't state the obvious
// Get patient by id
var patient = await _context.Patients.FindAsync(id);

// ✅ Comment design decisions
// Design Decision: Each user has exactly ONE role at a time.
// Remove all current roles before assigning the new one.
var currentRoles = await _userManager.GetRolesAsync(user);
await _userManager.RemoveFromRolesAsync(user, currentRoles);
```

### 5.2 — XML Documentation (Public APIs Only)

```csharp
// ✅ Add XML docs to service interfaces (the contract)
public interface IPatientService
{
    /// <summary>
    /// Returns paginated list of active patients. Soft-deleted patients are excluded.
    /// </summary>
    Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(PaginationRequest pagination);
}

// ❌ Don't add XML docs to private methods or obvious properties
```

### 5.3 — TODO Comments

```csharp
// ✅ Use TODO for known improvements with context
// TODO: Add caching for doctors list — low change frequency (Sprint 7?)

// ❌ Don't leave vague TODOs
// TODO: fix this later
```

---

## 6 — Git Conventions

### 6.1 — Commit Messages (Conventional Commits)

```markdown
Format: type: description (lowercase, no period, max 72 chars)

Types:
  feat:     New feature              → feat: add patient registration endpoint
  fix:      Bug fix                  → fix: handle duplicate email in doctor creation
  test:     Adding/updating tests    → test: add appointment overlap detection tests
  docs:     Documentation            → docs: update README with API endpoints
  refactor: Code change (no feature) → refactor: extract overlap detection into private method
  chore:    Maintenance              → chore: update NuGet packages
  style:    Formatting only          → style: fix indentation in PatientService
```

### 6.2 — Branching Strategy

```markdown
main      → Production-ready code. Protected by branch rules. Never push directly.
develop   → Active development. All work merges here first via PR.
feature/* → Optional feature branches for larger changes.

Workflow:
1. Work on develop (or feature branch)
2. Push to develop
3. Create Pull Request: develop → main
4. CI must pass before merge
5. Merge via GitHub UI
```

### 6.3 — What Never Gets Committed

```markdown
✅ In .gitignore:
  **/bin/
  **/obj/
  **/appsettings.Development.json
  *.user
  .vs/
  coverage-report/

✅ In User Secrets (never in any file):
  - Connection strings
  - JWT secret keys
  - Any passwords or API keys
```

---

## 7 — Testing Conventions

### 7.1 — Test Naming

```csharp
// Pattern: MethodName_Scenario_ExpectedResult
[Fact]
public async Task CreateAsync_WithValidData_ReturnsSuccessResult()

[Fact]
public async Task CreateAsync_WithDuplicateEmail_ReturnsFailureResult()

[Fact]
public async Task DeleteAsync_WithValidId_SetsIsDeletedTrue()
```

### 7.2 — Test Structure (Arrange-Act-Assert)

```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsSuccessResult()
{
    // Arrange — set up test data
    var patient = new Patient { Id = 1, FullName = "Ahmed Ali", Email = "ahmed@mail.com" };
    await _context.Patients.AddAsync(patient);
    await _context.SaveChangesAsync();

    // Act — call the method being tested
    var result = await _service.GetByIdAsync(1);

    // Assert — verify the result
    Assert.True(result.IsSuccess);
    Assert.Equal("Ahmed Ali", result.Value!.FullName);
}
```

### 7.3 — Test Organization

```markdown
Each test class mirrors the service it tests:
  PatientService.cs      → PatientServiceTests.cs
  AuthService.cs         → AuthServiceTests.cs
  PatientEndpoints.cs    → PatientEndpointsTests.cs

Inside each test class, group tests by method:
  // --- GetAllAsync ---
  // --- GetByIdAsync ---
  // --- CreateAsync ---
  // --- UpdateAsync ---
  // --- DeleteAsync ---
```

---

## Enforcement Summary

| Standard                         | Enforced By                                         | Level                           |
|----------------------------------|-----------------------------------------------------|---------------------------------|
| Naming conventions               | `.editorconfig` + `TreatWarningsAsErrors`           | Build breaks on violation       |
| Formatting (indentation, braces) | `.editorconfig`                                     | Auto-formatted on save          |
| Nullable reference types         | `Directory.Build.props` (`<Nullable>enable`)        | Build warning → error           |
| All warnings as errors           | `Directory.Build.props` (`<TreatWarningsAsErrors>`) | Build breaks on any warning     |
| NuGet version consistency        | `Directory.Packages.props` (CPM)                    | Restore fails on mismatch       |
| Code style (var, patterns)       | `.editorconfig` suggestions                         | IDE suggestions (some enforced) |
| Architecture patterns            | This document                                       | Developer discipline            |
| Git conventions                  | This document + Branch Protection                   | PR blocked if CI fails          |
| Test conventions                 | This document                                       | Code review                     |
