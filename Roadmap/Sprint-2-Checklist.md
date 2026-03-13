# Sprint 2 — Authentication (JWT + Refresh Token + Roles Setup)

**Project:** Clinic Management API  
**Sprint Duration:** 1 Week  
**Expected Time:** ~6 hours  
**Goal:** A fully secured authentication system with Register, Login, JWT, Refresh Token, and Roles setup  
**Prerequisites:** Sprint 1 is complete and CI is green

---

## Section 1 — Identity Setup

**Expected Time: 30 minutes**  
**Goal:** Register ASP.NET Core Identity in the project and connect it to the Database

```markdown
[✅] Add Identity package to Core project
    Command: dotnet add ClinicManagementAPI.Core package Microsoft.AspNetCore.Identity.EntityFrameworkCore
    ⚠️ Note: Package added to Core (not Api) because ApplicationUser lives in Core
             Api accesses it through project reference automatically

[✅] Add SqlServer package to Core project
    Command: dotnet add ClinicManagementAPI.Core package Microsoft.EntityFrameworkCore.SqlServer
    ⚠️ Note: Required for Migration — without it, SqlServerModelBuilderExtensions error occurs

[✅] Create ApplicationUser model in Core/Models/
    Command: New-Item -Path "ClinicManagementAPI.Core\Models" -Name "ApplicationUser.cs"
    Inherits: IdentityUser (provides Id, Email, PasswordHash, UserName, PhoneNumber, etc.)
    Extra fields:
    - FullName (string) — not in IdentityUser, specific to our Clinic
    - CreatedAt (DateTime) — defaults to DateTime.UtcNow

[✅] Update AppDbContext to inherit from IdentityDbContext<ApplicationUser>
    Location: Core/Data/AppDbContext.cs
    Changed: DbContext → IdentityDbContext<ApplicationUser>
    Added usings:
    - Microsoft.AspNetCore.Identity.EntityFrameworkCore
    - ClinicManagementAPI.Core.Models
    Result: Auto-generates 7 Identity tables (AspNetUsers, AspNetRoles, etc.)

[✅] Register Identity in Program.cs
    Added usings:
    - ClinicManagementAPI.Core.Models
    - Microsoft.AspNetCore.Identity
    Code:
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<AppDbContext>()
                    .AddDefaultTokenProviders()

[✅] Fix Connection String for SQL Server Express
    Command: dotnet user-secrets set "ConnectionStrings:ClinicDb"
             "Server=localhost\SQLEXPRESS;Database=ClinicDb;Trusted_Connection=True;TrustServerCertificate=True;"
             --project ClinicManagementAPI.Api
    ⚠️ Note: Original had Server=localhost — must be localhost\SQLEXPRESS for Express edition

[✅] Add Identity Migration
    Command: dotnet ef migrations add AddIdentity --project ClinicManagementAPI.Core
                                                  --startup-project ClinicManagementAPI.Api

[✅] Apply Migration to Database
    Command: dotnet ef database update --project ClinicManagementAPI.Core
                                       --startup-project ClinicManagementAPI.Api
    Result: Created ClinicDb database + 7 Identity tables
    Tables: AspNetUsers, AspNetRoles, AspNetRoleClaims, AspNetUserClaims,
            AspNetUserLogins, AspNetUserRoles, AspNetUserTokens
```

**Why Identity?**

```markdown
Manual implementation → You handle password hashing, tokens, security ❌ (risky)
ASP.NET Core Identity → Microsoft handles all security concerns ✅ (industry standard)
```

---

## Section 2 — JWT Configuration

**Expected Time: 30 minutes**  
**Goal:** Configure JWT settings securely — secret key never goes to GitHub

```markdown
[✅] Add JWT settings placeholder in appsettings.json (safe to commit)
    Added "Jwt" section with: Issuer, Audience, ExpiryMinutes (60), RefreshTokenExpiryDays (7)
    ⚠️ Note: Secret Key is NOT here — it's in User Secrets only

[✅] JWT Secret Key already configured via User Secrets (Sprint 1)
    Command: dotnet user-secrets set "Jwt:Key" "your-super-secret-key-min-32-characters"
             --project ClinicManagementAPI.Api

[✅] Create JwtSettings class in Core/Models/
    Command: New-Item -Path "ClinicManagementAPI.Core\Models" -Name "JwtSettings.cs"
    Properties: Key, Issuer, Audience, ExpiryMinutes (int), RefreshTokenExpiryDays (int)
    Purpose: Bind JSON settings to a C# object — avoids magic strings

[✅] Register JWT Authentication in Program.cs
    Added usings:
    - System.Text
    - Microsoft.AspNetCore.Authentication.JwtBearer
    - Microsoft.IdentityModel.Tokens
    Code:
    - Bind JwtSettings from Configuration using GetSection("Jwt").Get<JwtSettings>()
    - Register JwtSettings as Singleton for DI
    - AddAuthentication with JwtBearerDefaults.AuthenticationScheme
    - AddJwtBearer with TokenValidationParameters (Issuer, Audience, SigningKey, Lifetime)

[✅] Add app.UseAuthentication() and app.UseAuthorization() in Program.cs
    Location: after app.UseHttpsRedirection()
    ⚠️ Note: Order matters!
    app.UseAuthentication()  → "Who are you?" must come first
    app.UseAuthorization()   → "Are you allowed?" must come second
```

**Why User Secrets for JWT Key?**

```markdown
JWT Key in appsettings.json → Visible on GitHub → Anyone can forge tokens ❌
JWT Key in User Secrets     → Local only → Tokens are safe ✅
```

---

## Section 3 — Roles Setup

**Expected Time: 20 minutes**  
**Goal:** Define roles from day one — will be applied to endpoints in Sprint 3

```markdown
[✅] Create AppRoles static class in Core/Models/
    Command: New-Item -Path "ClinicManagementAPI.Core\Models" -Name "AppRoles.cs"
    Roles (4 instead of 3 — added Doctor as essential clinic role):
    - Admin
    - Doctor
    - Receptionist
    - Patient
    ⚠️ Note: Applied YAGNI principle — only added roles needed now
             Other roles (Nurse, Pharmacist, etc.) will be added in future sprints

[✅] Create DatabaseSeeder in Core/Data/
    Command: New-Item -Path "ClinicManagementAPI.Core\Data" -Name "DatabaseSeeder.cs"
    Static method SeedRolesAsync(RoleManager<IdentityRole>)
    Logic: loops through AppRoles, checks RoleExistsAsync → creates if missing
    Safe to run multiple times — skips existing roles

[✅] Call DatabaseSeeder in Program.cs after app.Build()
    Added using: ClinicManagementAPI.Core.Data
    Code: Creates a scope → gets RoleManager from DI → calls SeedRolesAsync
    ⚠️ Note: Uses "using" block to dispose the scope after seeding

[✅] Add Authorization service in Program.cs
    Command: builder.Services.AddAuthorization()
    ⚠️ Note: Was missing — required by app.UseAuthorization()

[✅] Verified roles in database
    Command: sqlcmd -S localhost\SQLEXPRESS -d ClinicDb -E -Q "SELECT Name FROM AspNetRoles"
    Result: Admin, Doctor, Receptionist, Patient ✅
```

**Why seed roles now?**

```markdown
Roles defined now  → Register endpoint can assign roles immediately ✅
Roles defined later → Need to revisit Auth code in Sprint 3 ❌
```

---

## Section 4 — DTOs + Input Validation

**Expected Time: 30 minutes**  
**Goal:** Define the request and response shapes with proper validation — reject bad data before it reaches your services

```markdown
[✅] Create DTOs folder structure
    Command: New-Item -Path "ClinicManagementAPI.Api\DTOs\Auth" -ItemType Directory -Force

[✅] Create RegisterRequest DTO in Api/DTOs/Auth/
    Command: New-Item -Path "ClinicManagementAPI.Api\DTOs\Auth" -Name "RegisterRequest.cs"
    Properties:
    - FullName (string, [Required], [MinLength(2)], [MaxLength(100)])
    - Email (string, [Required], [EmailAddress])
    - Password (string, [Required], [MinLength(8)])
    ⚠️ Note: No Role field — all new users register as Patient by default
             Only Admin can assign other roles (Sprint 3)

[✅] Create LoginRequest DTO in Api/DTOs/Auth/
    Command: New-Item -Path "ClinicManagementAPI.Api\DTOs\Auth" -Name "LoginRequest.cs"
    Properties:
    - Email (string, [Required], [EmailAddress])
    - Password (string, [Required])
    ⚠️ Note: No [MinLength] on Password — length was validated at Register
             Here we only check it exists, service verifies correctness

[✅] Create AuthResponse DTO in Api/DTOs/Auth/
    Command: New-Item -Path "ClinicManagementAPI.Api\DTOs\Auth" -Name "AuthResponse.cs"
    Properties:
    - AccessToken (string)
    - RefreshToken (string)
    - ExpiresAt (DateTime)
    ⚠️ Note: No validation attributes — this is a response, not a request

[✅] Create ValidationFilter for Minimal APIs in Api/Filters/
    Command: New-Item -Path "ClinicManagementAPI.Api\Filters" -Name "ValidationFilter.cs" -Force
    Generic filter: ValidationFilter<T> implements IEndpointFilter
    Logic: Checks request body exists → runs Data Annotations manually → returns 400 if invalid
    Usage: .AddEndpointFilter<ValidationFilter<RegisterRequest>>() on endpoints
    ⚠️ Note: Minimal APIs don't auto-validate Data Annotations like Controllers
             This filter handles it manually before request reaches the handler
```

**Why Input Validation?**

```markdown
Without validation → Empty emails, 1-char passwords reach your service layer ❌
With validation    → Bad requests are rejected at the door with clear error messages ✅
```

**Why no Role in RegisterRequest?**

```markdown
User picks their own role → Anyone can register as Admin → Security hole ❌
Default role = Patient    → Only Admin promotes users    → Secure by default ✅
```

---

## Section 5 — Register Endpoint (using Result Pattern)

**Expected Time: 45 minutes**  
**Goal:** Allow new users to register — always as Patient role by default

```markdown
[✅] Move DTOs from Api to Core (architecture fix)
    Command: New-Item -Path "ClinicManagementAPI.Core\DTOs\Auth" -ItemType Directory -Force
    Commands:
    - Move-Item RegisterRequest.cs, LoginRequest.cs, AuthResponse.cs to Core\DTOs\Auth\
    - Changed namespace from ClinicManagementAPI.Api.DTOs.Auth → ClinicManagementAPI.Core.DTOs.Auth
    - Remove-Item -Path "ClinicManagementAPI.Api\DTOs" -Recurse
    ⚠️ Note: DTOs moved to Core because IAuthService needs them
             Keeping them in Api would cause circular dependency (Api → Core → Api)

[✅] Create IAuthService interface in Core/Interfaces/
    Command: New-Item -Path "ClinicManagementAPI.Core\Interfaces" -Name "IAuthService.cs" -Force
    Methods (all return Result<T>):
    - Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    - Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    - Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
    - Task<Result<bool>> RevokeTokenAsync(string refreshToken)

[✅] Create AuthService in Core/Services/
    Command: New-Item -Path "ClinicManagementAPI.Core\Services" -Name "AuthService.cs" -Force
    Implements IAuthService
    RegisterAsync logic:
    - Check if email already exists → Result.Failure("Email already registered", 400)
    - Create user via UserManager → if failed, Result.Failure(errors, 400)
    - Assign "Patient" role by default (using AppRoles.Patient)
    - Generate JWT Token (using GenerateJwtToken private method)
    - Generate Refresh Token (using GenerateRefreshToken private method)
    - Return Result.Success(new AuthResponse { ... })
    Private helpers:
    - GenerateJwtToken: creates JWT with claims (Sub, Email, Jti, FullName), signs with HmacSha256
    - GenerateRefreshToken: generates 64-byte cryptographically secure random string
    ⚠️ Note: LoginAsync, RefreshTokenAsync, RevokeTokenAsync throw NotImplementedException
             Will be implemented in Sections 6 and 7

[✅] Register IAuthService in Program.cs
    Added usings: ClinicManagementAPI.Core.Interfaces, ClinicManagementAPI.Core.Services
    Code: builder.Services.AddScoped<IAuthService, AuthService>()

[✅] Create Auth endpoints file in Api/Endpoints/AuthEndpoints.cs
    Command: New-Item -Path "ClinicManagementAPI.Api\Endpoints" -Name "AuthEndpoints.cs" -Force
    Extension method MapAuthEndpoints on WebApplication
    Uses MapGroup("/api/auth") to group all auth endpoints
    POST /api/auth/register:
    - Accepts RegisterRequest (from body) + IAuthService (from DI)
    - Calls AuthService.RegisterAsync()
    - result.IsSuccess → 201 Created + AuthResponse
    - result.IsFailure → Results.Problem with error details
    - AddEndpointFilter<ValidationFilter<RegisterRequest>>() for input validation

[✅] Map Auth endpoints in Program.cs
    Added using: ClinicManagementAPI.Api.Endpoints
    Code: app.MapAuthEndpoints() — added before app.Run()
```

---

## Section 6 — Login Endpoint (using Result Pattern)

**Expected Time: 30 minutes**  
**Goal:** Allow existing users to login and receive JWT + Refresh Token

```markdown
[✅] Add LoginAsync logic in AuthService
    Location: ClinicManagementAPI.Core\Services\AuthService.cs
    Logic:
    1. Find user by email using userManager.FindByEmailAsync
       → if null, return Result.Failure("Invalid credentials", 401)
    2. Verify password using userManager.CheckPasswordAsync
       → if failed, return Result.Failure("Invalid credentials", 401)
    3. Generate JWT Token using GenerateJwtToken
    4. Generate Refresh Token using GenerateRefreshToken
    5. Return Result.Success(new AuthResponse { ... })
    ⚠️ Note: SAME error message "Invalid credentials" for both cases
             Prevents email enumeration attacks — attacker can't tell if email exists

[✅] Add Login endpoint in AuthEndpoints.cs
    Location: ClinicManagementAPI.Api\Endpoints\AuthEndpoints.cs
    POST /api/auth/login
    - Accepts LoginRequest (from body) + IAuthService (from DI)
    - Calls AuthService.LoginAsync()
    - result.IsSuccess → Results.Ok (200) + AuthResponse
    - result.IsFailure → Results.Problem with error details
    - AddEndpointFilter<ValidationFilter<LoginRequest>>() for input validation
    ⚠️ Note: Uses Ok (200) not Created (201) — Login doesn't create a new resource
```

**Why same error message?**

```markdown
"User not found" vs "Wrong password" → Attacker knows which emails exist ❌
"Invalid credentials" for both       → Attacker learns nothing ✅
```

---

## Section 7 — Refresh Token (using Result Pattern)

**Expected Time: 60 minutes**  
**Goal:** Allow users to get a new JWT Token without logging in again

```markdown
[✅] Create RefreshToken model in Core/Models/
    Command: New-Item -Path "ClinicManagementAPI.Core\Models" -Name "RefreshToken.cs"
    Properties:
    - Id (int) — auto-increment PK
    - Token (string) — the random Base64 string
    - ExpiresAt (DateTime)
    - CreatedAt (DateTime) — defaults to DateTime.UtcNow
    - IsRevoked (bool) — marks token as cancelled
    - UserId (string → FK to ApplicationUser)
    - User (ApplicationUser) — navigation property

[✅] Add RefreshTokens DbSet + Configuration to AppDbContext
    Location: Core/Data/AppDbContext.cs
    Added: public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    Added OnModelCreating override:
    - base.OnModelCreating(modelBuilder) — required for Identity tables
    - HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Restrict)
    - HasIndex(rt => rt.Token).IsUnique() — fast lookups + no duplicates

[✅] Add RefreshToken Migration
    Command: dotnet ef migrations add AddRefreshTokens --project ClinicManagementAPI.Core
                                                       --startup-project ClinicManagementAPI.Api

[✅] Apply Migration
    Command: dotnet ef database update --project ClinicManagementAPI.Core
                                       --startup-project ClinicManagementAPI.Api
    Verified: sqlcmd confirms 6 columns (Id int, Token nvarchar, ExpiresAt datetime2,
              CreatedAt datetime2, IsRevoked bit, UserId nvarchar)

[✅] Add AppDbContext to AuthService constructor
    Changed: AuthService(UserManager, JwtSettings) → AuthService(UserManager, JwtSettings, AppDbContext)
    Added usings: ClinicManagementAPI.Core.Data, Microsoft.EntityFrameworkCore

[✅] Update RegisterAsync + LoginAsync to save Refresh Token in Database
    Added to both methods after GenerateRefreshToken():
    - dbContext.RefreshTokens.Add(new RefreshToken { Token, UserId, ExpiresAt, CreatedAt })
    - await dbContext.SaveChangesAsync()
    ⚠️ Note: Previously tokens were generated but never stored — now they're persisted

[✅] Add RefreshTokenAsync logic in AuthService
    Logic (7 steps):
    1. Find token in Database using FirstOrDefaultAsync
       → if null, return Result.Failure("Invalid token", 401)
    2. Validate it is not expired and not revoked
       → if invalid, return Result.Failure("Token expired or revoked", 401)
    3. Revoke old token (IsRevoked = true)
    4. Get user via userManager.FindByIdAsync(storedToken.UserId)
    5. Generate new JWT + Refresh Token
    6. Save new Refresh Token in Database (with expiry from JwtSettings.RefreshTokenExpiryDays)
    7. Return Result.Success(new AuthResponse { ... })
    ⚠️ Note: Token Rotation — old token revoked, new one issued every refresh

[✅] Add RevokeTokenAsync logic in AuthService
    Logic (3 steps):
    1. Find token in Database
       → if null, return Result.Failure("Invalid token", 400)
    2. Mark it as revoked + SaveChangesAsync
    3. Return Result.Success(true)
    ⚠️ Note: No expiry/revoked check — logout should succeed even for expired tokens
             400 not 401 — user is authenticated, the token string is just invalid

[✅] Create RefreshTokenRequest DTO in Core/DTOs/Auth/
    Command: New-Item -Path "ClinicManagementAPI.Core\DTOs\Auth" -Name "RefreshTokenRequest.cs"
    Properties: RefreshToken (string, [Required])
    ⚠️ Note: Minimal API can't bind raw string from body — needs a DTO object

[✅] Add Refresh and Logout endpoints in AuthEndpoints.cs
    POST /api/auth/refresh
    - Accepts: RefreshTokenRequest (from body) + IAuthService (from DI)
    - Calls AuthService.RefreshTokenAsync(request.RefreshToken)
    - result.IsSuccess → 200 + new AuthResponse
    - result.IsFailure → Results.Problem(result.Error, statusCode: result.StatusCode)
    - AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>()

    POST /api/auth/logout
    - Accepts: RefreshTokenRequest (from body) + IAuthService (from DI)
    - Requires valid JWT Token → .RequireAuthorization()
    - Calls AuthService.RevokeTokenAsync(request.RefreshToken)
    - result.IsSuccess → 204 NoContent
    - result.IsFailure → Results.Problem(result.Error, statusCode: result.StatusCode)
    - AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>()
    - ⚠️ Named "logout" not "revoke" — this is what frontend developers expect
```

**Why Refresh Token?**

```markdown
Without Refresh Token → User must login again every 60 minutes ❌
With Refresh Token    → User gets new JWT silently → Better UX ✅
```

**Why configurable expiry?**

```markdown
Hardcoded expiry   → Need to redeploy to change it ❌
Config-based expiry → Change appsettings.json and restart → Flexible ✅
```

**Why Token Rotation?**

```markdown
Reuse same refresh token → If stolen, attacker has permanent access ❌
New token each refresh   → Old token dies, stolen token is useless ✅
```

---

## Section 8 — Tests

**Expected Time: 60 minutes**  
**Goal:** 70%+ coverage on Auth logic — catch bugs before they reach production

```markdown
[✅] Create Unit Tests in ClinicManagementAPI.Tests/Unit/AuthServiceTests.cs
    Setup: DI container with AddLogging + InMemory DB + Identity + Role Seeding
    Cleanup: IDisposable — EnsureDeleted + Dispose + GC.SuppressFinalize
    Test cases (12 tests):
    - RegisterAsync_WithValidData_ReturnsSuccessResult
    - RegisterAsync_WithExistingEmail_ReturnsFailureResult
    - RegisterAsync_WithWeakPassword_ReturnsFailureResult
    - LoginAsync_WithValidCredentials_ReturnsSuccessResult
    - LoginAsync_WithWrongPassword_ReturnsFailureResult
    - LoginAsync_WithNonExistentEmail_ReturnsFailureResult (same error as wrong password)
    - RefreshTokenAsync_WithValidToken_ReturnsSuccessResult
    - RefreshTokenAsync_WithExpiredToken_ReturnsFailureResult
    - RefreshTokenAsync_WithNonExistentToken_ReturnsFailureResult
    - RefreshTokenAsync_WithRevokedToken_ReturnsFailureResult
    - RevokeTokenAsync_WithValidToken_ReturnsSuccessResult
    - RevokeTokenAsync_WithNonExistentToken_ReturnsFailureResult
    ⚠️ Note: Roles must be seeded manually in tests — no Program.cs runs in Unit Tests
             InMemory DB with Guid.NewGuid() ensures test isolation

[✅] Create CustomWebApplicationFactory in ClinicManagementAPI.Tests/Integration/
    Location: ClinicManagementAPI.Tests/Integration/CustomWebApplicationFactory.cs
    Inherits: WebApplicationFactory<Program>
    Logic: Removes real SQL Server → Adds InMemory Database
    Uses: ConfigureTestServices + RemoveAll for clean replacement
    ⚠️ Note: Required "public partial class Program { }" in Api/Program.cs

[✅] Create Integration Tests in ClinicManagementAPI.Tests/Integration/AuthEndpointsTests.cs
    Setup: IClassFixture<CustomWebApplicationFactory> — shared server across all tests
    Uses: HttpClient with PostAsJsonAsync for JSON requests
    Test cases (12 tests):
    - POST /api/auth/register → 201 with valid data
    - POST /api/auth/register → 400 with missing fields (ValidationFilter)
    - POST /api/auth/register → 400 with invalid email format (ValidationFilter)
    - POST /api/auth/register → 400 with short password < 8 chars (ValidationFilter)
    - POST /api/auth/register → 400 with duplicate email
    - POST /api/auth/login    → 200 with valid credentials
    - POST /api/auth/login    → 401 with wrong password
    - POST /api/auth/login    → 401 with non-existent email (same error)
    - POST /api/auth/refresh  → 200 with valid refresh token
    - POST /api/auth/refresh  → 401 with invalid token
    - POST /api/auth/logout   → 204 on success (requires Bearer token in header)
    - POST /api/auth/logout   → 401 without JWT (unauthorized)
    ⚠️ Note: Guid.NewGuid() in emails prevents conflicts — all tests share one InMemory DB
             Logout test uses HttpRequestMessage to add Authorization header manually

[✅] Run all tests and verify they pass
    Command: dotnet test --verbosity normal
    Result: 24 tests passed (12 Unit + 12 Integration), 0 failed

[✅] Check coverage
    Command: dotnet test --collect:"XPlat Code Coverage"
    Tool: dotnet tool install -g dotnet-reportgenerator-globaltool
    Report: reportgenerator -reports:TestResults\*\coverage.cobertura.xml
                            -targetdir:coveragereport -reporttypes:TextSummary
    Result: AuthService 100%, AuthEndpoints 100%, AppDbContext 100%, DatabaseSeeder 100%
            All DTOs and Models 100% — overall Auth logic coverage well above 70% target
    ⚠️ Note: Low overall % (17.3%) is due to auto-generated code (Migrations, OpenApi, etc.)
             Add coveragereport/ and **/TestResults/ to .gitignore
```

**Why Unit + Integration?**

```markdown
Unit Tests only   → Business logic works but endpoints might be broken ❌
Integration only  → Endpoints work but can't pinpoint which service method failed ❌
Both together     → Full confidence from service layer to HTTP response ✅
```

---

## Section 9 — CI Update

**Expected Time: 15 minutes**  
**Goal:** CI pipeline now runs Auth tests automatically on every push

```markdown
[✅] Verify build.yml runs dotnet test (already configured in Sprint 1)

[✅] Add coverage report step to build.yml
    - name: Test with coverage
      run: dotnet test --collect:"XPlat Code Coverage" --verbosity normal

[✅] Push to GitHub and verify:
    ✅ Build passes
    ✅ All Auth tests pass in CI
```

---

## Sprint 2 — Done Definition

```markdown
✅ ASP.NET Core Identity is configured and connected to Database
✅ JWT settings are secured via User Secrets
✅ RefreshTokenExpiryDays is configurable in appsettings.json
✅ 3 Roles are seeded in Database (Admin, Receptionist, Patient)
✅ Input validation rejects bad data with clear 400 errors
✅ New users always register as Patient (no self-assigned Admin)
✅ All services use Result<T> pattern (no exceptions for business errors)
✅ POST /api/auth/register works and returns JWT + Refresh Token
✅ POST /api/auth/login works and returns JWT + Refresh Token
✅ POST /api/auth/login returns same error for wrong password and non-existent email
✅ POST /api/auth/refresh works and returns new JWT + Refresh Token
✅ POST /api/auth/logout works and invalidates Refresh Token
✅ All Unit Tests pass (testing Result success/failure)
✅ All Integration Tests pass (including validation edge cases)
✅ CI pipeline is green on GitHub
```

---

**Next Sprint:** Patients CRUD + Role-based Authorization
