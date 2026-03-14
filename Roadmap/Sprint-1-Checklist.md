# Sprint 1 — Project Setup + CI (Build)

**Project:** Clinic Management API  
**Sprint Duration:** 1 Week  
**Expected Time:** ~4.5 hours  
**Goal:** A running empty API connected to SQL Server, clean architecture from day one, and automated CI build on every push  
**Tech Stack:** .NET 10, Minimal API, EF Core, SQL Server, xUnit, GitHub Actions

---

## Section 1 — GitHub Repository (First Step)

**Expected Time: 10 minutes**  
**Goal:** Create the repo on GitHub first so git history starts from the very first line of code

```markdown
[✅] Create GitHub Repository on GitHub website
    Name: clinic-management-api
    Visibility: Public (for portfolio visibility)
    ✅ Add README.md
    ✅ Add .gitignore → select Visual Studio template

[✅] Clone the repo locally
    Command: git clone https://github.com/<your-username>/clinic-management-api.git

[✅] Navigate into the cloned folder
    Command: cd clinic-management-api
```

**Why repo first?**

```markdown
Repo first  → git is connected from day one → full commit history from first line ✅
Local first → need manual git init + remote setup → risk of messy history ❌
```

---

## Section 2 — README.md

**Expected Time: 15 minutes**  
**Goal:** First thing an interviewer sees when opening your GitHub — make it count

```markdown
[✅] Update README.md with the following sections:

    # Clinic Management API

    A RESTful API built with ASP.NET Core Minimal API for managing clinic
    appointments, patients, and doctors.

    ## Tech Stack
    - .NET 10 / ASP.NET Core Minimal API
    - Entity Framework Core + SQL Server
    - JWT Authentication
    - xUnit + GitHub Actions CI

    ## How to Run
    1. Clone the repository
    2. Configure User Secrets (see below)
    3. Run: dotnet run --project ClinicManagementAPI.Api

    ## User Secrets Setup
    dotnet user-secrets set "ConnectionStrings:ClinicDb" "your-connection-string"
    dotnet user-secrets set "Jwt:Key" "your-secret-key"

    ## API Endpoints
    (Will be updated each sprint)
```

---

## Section 3 — Solution Structure

**Expected Time: 30 minutes**  
**Goal:** Create the solution with 3 separated projects for clean architecture from day one

```markdown
[✅] Create the Solution
    Command: dotnet new sln -n ClinicManagementAPI

[✅] Create the Web API Project (Minimal API)
    Command: dotnet new webapi -n ClinicManagementAPI.Api --use-minimal-apis

[✅] Create the Core Project (Business Logic - Class Library)
    Command: dotnet new classlib -n ClinicManagementAPI.Core

[✅] Create the Test Project
    Command: dotnet new xunit -n ClinicManagementAPI.Tests

[✅] Add all projects to the Solution
    Command: dotnet sln add ClinicManagementAPI.Api
    Command: dotnet sln add ClinicManagementAPI.Core
    Command: dotnet sln add ClinicManagementAPI.Tests

[✅] Add reference from Api to Core
    Command: dotnet add ClinicManagementAPI.Api reference ClinicManagementAPI.Core

[✅] Add reference from Tests to Core
    Command: dotnet add ClinicManagementAPI.Tests reference ClinicManagementAPI.Core

[✅] Add reference from Tests to Api
    Command: dotnet add ClinicManagementAPI.Tests reference ClinicManagementAPI.Api
```

**Why 3 projects?**

```markdown
ClinicManagementAPI.Api      → Endpoints, DTOs, Program.cs (knows about Web)
ClinicManagementAPI.Core     → Models, Services, Interfaces (knows nothing about Web)
ClinicManagementAPI.Tests    → Tests for both projects
```

---

## Section 4 — Shared Build Settings (Directory.Build.props)

**Expected Time: 10 minutes**  
**Goal:** One file controls shared settings for ALL projects — no repetition in each .csproj

```markdown
[✅] Create Directory.Build.props in the Solution root folder
    Location: /clinic-management-api/Directory.Build.props
    Command: touch Directory.Build.props
    Content:
```

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

```markdown
[✅] Remove duplicate properties from each .csproj file
    After creating Directory.Build.props, remove TargetFramework,
    Nullable, and ImplicitUsings from:
    - ClinicManagementAPI.Api.csproj
    - ClinicManagementAPI.Core.csproj
    - ClinicManagementAPI.Tests.csproj

[✅] Verify the solution still builds
    Command: dotnet build
```

**Why Directory.Build.props?**

```markdown
Without it → Same settings repeated in 3 .csproj files → Easy to forget one ❌
With it    → One file controls all projects → Consistent and DRY ✅
TreatWarningsAsErrors → Forces you to write clean code from day one ✅
```

---

## Section 5 — Central Package Management (CPM)

**Expected Time: 20 minutes**  
**Goal:** One single place to control all NuGet package versions across all projects

```markdown
[✅] Create Directory.Packages.props in the Solution root folder
    Location: /clinic-management-api/Directory.Packages.props
    Command: touch Directory.Packages.props
    Content:
```

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <MicrosoftExtensionsVersion>10.0.3</MicrosoftExtensionsVersion>
  </PropertyGroup>

  <ItemGroup Label="For EF Core.">
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="$(MicrosoftExtensionsVersion)" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="$(MicrosoftExtensionsVersion)" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="$(MicrosoftExtensionsVersion)" />
  </ItemGroup>

  <ItemGroup Label="For authentication.">
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="$(MicrosoftExtensionsVersion)" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="$(MicrosoftExtensionsVersion)" />
  </ItemGroup>

  <ItemGroup Label="For web services.">
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="$(MicrosoftExtensionsVersion)" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.12.46" />
  </ItemGroup>

  <ItemGroup Label="For health checks.">
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="$(MicrosoftExtensionsVersion)" />
  </ItemGroup>

  <ItemGroup Label="For unit testing.">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="$(MicrosoftExtensionsVersion)" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
</Project>
```

```markdown
[✅] Install packages in API project (no version needed — CPM handles it)
    Command: dotnet add ClinicManagementAPI.Api package Microsoft.EntityFrameworkCore.SqlServer
    Command: dotnet add ClinicManagementAPI.Api package Microsoft.EntityFrameworkCore.Design
    Command: dotnet add ClinicManagementAPI.Api package Microsoft.AspNetCore.Authentication.JwtBearer
    Command: dotnet add ClinicManagementAPI.Api package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore

[✅] Install packages in Test project
    Command: dotnet add ClinicManagementAPI.Tests package Microsoft.EntityFrameworkCore.InMemory
    Command: dotnet add ClinicManagementAPI.Tests package Microsoft.AspNetCore.Mvc.Testing

[✅] Verify all versions are consistent across projects
    Command: dotnet restore
```

**Why CPM?**

```markdown
Without CPM → Each project sets its own version → Version conflicts between Api and Tests
With CPM    → One file controls all versions → No conflicts, easy to update
```

---

## Section 6 — EditorConfig (Code Style)

**Expected Time: 5 minutes**  
**Goal:** Consistent code style across the entire project — anyone opening the repo sees the same formatting

```markdown
[✅] Generate .editorconfig in the Solution root folder
    Command: dotnet new editorconfig

[✅] Verify .editorconfig was created
    Location: /clinic-management-api/.editorconfig
```

**Why EditorConfig?**

```markdown
Without it → Each developer's IDE uses different formatting → Messy diffs ❌
With it    → Everyone follows the same style → Clean diffs, professional code ✅
Interviewers notice → Consistent style shows attention to detail ✅
```

---

## Section 7 — Folder Structure

**Expected Time: 10 minutes**  
**Goal:** Every file has a clear home before we write a single line of business code

```markdown
ClinicManagementAPI.Api/
├── Endpoints/       → Minimal API endpoint definitions
├── DTOs/            → Request and Response objects
├── Middleware/       → Custom middleware (error handling, etc.)
└── Program.cs       → App entry point and service registration

ClinicManagementAPI.Core/
├── Models/          → Entity classes + Result<T> + AppRoles
├── Data/            → AppDbContext and DB configurations
├── Services/        → Business logic
└── Interfaces/      → Service contracts

ClinicManagementAPI.Tests/
├── Unit/            → Unit tests for Services
└── Integration/     → Integration tests for Endpoints

[✅] Create Endpoints folder in Api project
[✅] Create DTOs folder in Api project
[✅] Create Middleware folder in Api project
[✅] Create Models folder in Core project
[✅] Create Data folder in Core project
[✅] Create Services folder in Core project
[✅] Create Interfaces folder in Core project
[✅] Create Unit folder in Tests project
[✅] Create Integration folder in Tests project
```

---

## Section 8 — Result Pattern (Service Response Foundation)

**Expected Time: 20 minutes**  
**Goal:** A clean way for services to say "I succeeded" or "I failed" — without throwing exceptions for expected errors

```markdown
[✅] Create Result<T> class in Core/Models/Result.cs
    Properties:
    - IsSuccess (bool)
    - Value (T?) — the data when successful
    - Error (string?) — the error message when failed
    - StatusCode (int) — HTTP status code to return (200, 400, 401, 404, etc.)

    Static factory methods:
    - Result<T>.Success(T value)
    - Result<T>.Failure(string error, int statusCode = 400)

[✅] Verify the class compiles
    Command: dotnet build
```

**Example of how `Result<T>` will be used (Sprint 2):**

```csharp
// In AuthService (Sprint 2):
public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user is null)
        return Result<AuthResponse>.Failure("Invalid credentials", 401);

    return Result<AuthResponse>.Success(new AuthResponse { ... });
}

// In Endpoint (Sprint 2):
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService auth) =>
{
    var result = await auth.LoginAsync(request);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Problem(result.Error, statusCode: result.StatusCode);
});
```

**Why Result Pattern?**

```markdown
Exceptions for business errors → "Wrong password" is not exceptional → Misuse of exceptions ❌
Result Pattern                 → Expected errors handled explicitly → Clean and predictable ✅
Global Error Handler           → Still catches unexpected errors (DB crash, null ref) ✅
Together                       → Expected errors = Result, Unexpected errors = Global Handler ✅
```

---

## Section 9 — Database Connection + User Secrets + appsettings

**Expected Time: 30 minutes**  
**Goal:** Connect the API to SQL Server securely — no sensitive data on GitHub ever

```markdown
[✅] Initialize User Secrets in API project
    Command: dotnet user-secrets init --project ClinicManagementAPI.Api

[✅] Add Connection String via User Secrets
    Command: dotnet user-secrets set "ConnectionStrings:ClinicDb"
             "Server=localhost;Database=ClinicDb;Trusted_Connection=True;TrustServerCertificate=True;"
             --project ClinicManagementAPI.Api

[✅] Update appsettings.json (safe placeholder — gets committed)
    {
      "ConnectionStrings": {
        "ClinicDb": "CONFIGURED_VIA_USER_SECRETS"
      }
    }

[✅] Create appsettings.Development.json (local overrides — never committed)
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      }
    }

[✅] Verify .gitignore includes these entries explicitly
    **/appsettings.Development.json
    *.user
    .vs/

[✅] Create AppDbContext (empty for now) in Core/Data/
    Location: ClinicManagementAPI.Core/Data/AppDbContext.cs
    command: New-Item ClinicManagementAPI.Core/Data/AppDbContext.cs
    Inherits: DbContext

[✅] Add EF Core package to Core project
    Command: dotnet add ClinicManagementAPI.Core package Microsoft.EntityFrameworkCore

[✅] Register AppDbContext in Program.cs
    Method: builder.Services.AddDbContext<AppDbContext>()

[✅] Verify the API starts with no errors
    Command: dotnet run --project ClinicManagementAPI.Api
```

**Why appsettings.Development.json?**

```markdown
appsettings.json            → Shared settings → Safe to commit ✅
appsettings.Development.json → Local overrides → Never committed ✅
User Secrets                → Sensitive data → Never committed ✅
```

---

## Section 10 — Global Error Handling

**Expected Time: 30 minutes**  
**Goal:** Every unexpected error returns a clean JSON response — never expose stack traces to clients

```markdown
[✅] Create GlobalExceptionHandler in Api/Middleware/
    command: New-Item ClinicManagementAPI.Api/Middleware/GlobalExceptionHandler.cs
    Implements: IExceptionHandler
    Logic:
    - Catch all unhandled exceptions
    - Log the full exception details using ILogger (for developers)
    - Return a clean JSON response to the client:
      {
        "status": 500,
        "title": "Internal Server Error",
        "detail": "Something went wrong. Please try again later."
      }

[✅] Register GlobalExceptionHandler in Program.cs
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>()
    builder.Services.AddProblemDetails()

[✅] Add UseExceptionHandler in Program.cs (before other middleware)
    app.UseExceptionHandler()

[✅] Test by throwing a temporary exception in a test endpoint
    Verify: API returns JSON error, not HTML stack trace
```

**Why Global Error Handling?**

```markdown
Without it → Unhandled exceptions show stack traces → Security risk + bad UX ❌
With it    → Clean JSON errors + full logs for debugging → Professional API ✅
Works with Result Pattern:
  Result<T>            → Handles expected errors (wrong password, invalid input)
  Global Error Handler → Handles unexpected errors (DB crash, null reference)
```

---

## Section 11 — Health Check Endpoint

**Expected Time: 15 minutes**  
**Goal:** A simple endpoint that tells you if the API and database are alive

```markdown
[✅] Register Health Checks in Program.cs
    builder.Services.AddHealthChecks()
                    .AddDbContextCheck<AppDbContext>("database")

[✅] Map Health Check endpoint in Program.cs
    app.MapHealthChecks("/health")

[✅] Test the endpoint
    GET /health → should return "Healthy" when DB is connected
    GET /health → should return "Unhealthy" when DB is down
```

**Why Health Checks?**

```markdown
Without it → You don't know if the API or DB is down until users complain ❌
With it    → Monitoring tools can ping /health every minute → instant alerts ✅
Interviewers love it → Shows you think about production readiness ✅
```

---

## Section 12 — GitHub Actions CI + Branch Protection

**Expected Time: 45 minutes**  
**Goal:** Every push triggers an automatic build — broken code is caught immediately

```markdown
[✅] Create GitHub Actions workflow file
    Command: New-Item -ItemType Directory -Path .github/workflows
    Command: New-Item .github/workflows/build.yml
    Location: .github/workflows/build.yml
    Content:

    name: CI Build

    on:
      push:
        branches: [ main, develop ]
      pull_request:
        branches: [ main ]

    jobs:
      build:
        runs-on: ubuntu-latest
        steps:
          - uses: actions/checkout@v4

          - name: Setup .NET
            uses: actions/setup-dotnet@v4
            with:
              dotnet-version: '10.0.x'

          - name: Restore dependencies
            run: dotnet restore

          - name: Build
            run: dotnet build --no-restore

          - name: Test
            run: dotnet test --no-build --verbosity normal

[✅] Push everything to GitHub
    Command: git add .
    Command: git commit -m "feat: initial project setup"
    Command: git push

[✅] Verify CI pipeline runs green on GitHub Actions tab

[✅] Enable Branch Protection on main branch
    GitHub website → Settings → Branches → Add rule
    Branch name: main
    ✅ Require status checks to pass before merging
    ✅ Require branches to be up to date before merging
    Select: "build" as required status check
```

**Why Branch Protection?**

```markdown
Without it → Broken code can be pushed directly to main ❌
With it    → GitHub blocks merge until CI passes ✅
```

---

## Section 13 — Diagrams

**Expected Time: 30 minutes**  
**Goal:** Create and update necessary system diagrams for Sprint 1 features (Project Setup, CI)

```markdown
[✅] Review docs/ to determine required diagrams for Sprint 1
[✅] Update/Create Component/Sequence Diagrams for project setup
[✅] Verify PlantUML/Markdown diagrams render correctly
```

---

## Sprint 1 — Done Definition

```markdown
✅ GitHub repo is live and cloned locally
✅ README.md is clear and professional
✅ Solution has 3 projects (Api, Core, Tests)
✅ Directory.Build.props controls shared settings (Nullable, TreatWarningsAsErrors)
✅ Directory.Packages.props controls all NuGet versions (no duplicates)
✅ .editorconfig enforces consistent code style
✅ Folder structure is clean and ready in all projects (including Middleware/)
✅ Result<T> class is ready in Core/Models/
✅ .gitignore explicitly includes appsettings.Development.json, *.user, .vs/
✅ Connection string is secured via User Secrets
✅ Global Error Handling returns clean JSON errors (unexpected errors)
✅ Health Check endpoint /health is working
✅ dotnet run starts the API with no errors
✅ GitHub Actions CI runs green on every push
✅ Branch Protection is enabled on main
✅ Diagrams for Sprint 1 are created and updated
```

---

**Next Sprint:** Authentication (JWT) + Tests
