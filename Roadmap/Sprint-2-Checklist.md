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
[ ] Create IAuthService interface in Core/Interfaces/
    Methods (all return Result<T>):
    - Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    - Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    - Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken)
    - Task<Result<bool>> RevokeTokenAsync(string refreshToken)

[ ] Create AuthService in Core/Services/
    Implements IAuthService
    RegisterAsync logic:
    - Check if email already exists
      → return Result.Failure("Email already registered", 400)
    - Create user via UserManager
      → if failed, return Result.Failure(errors, 400)
    - Assign "Patient" role by default (using AppRoles.Patient)
    - Generate JWT Token
    - Generate Refresh Token (with expiry from JwtSettings.RefreshTokenExpiryDays)
    - Return Result.Success(new AuthResponse { ... })

[ ] Register IAuthService in Program.cs
    builder.Services.AddScoped<IAuthService, AuthService>()

[ ] Create Auth endpoints file in Api/Endpoints/AuthEndpoints.cs
    POST /api/auth/register
    - Accepts RegisterRequest
    - Calls AuthService.RegisterAsync()
    - Maps Result to HTTP response:
      result.IsSuccess → 201 + AuthResponse
      result.IsFailure → Results.Problem(result.Error, statusCode: result.StatusCode)

[ ] Map Auth endpoints in Program.cs
    app.MapAuthEndpoints()
```

---

## Section 6 — Login Endpoint (using Result Pattern)

**Expected Time: 30 minutes**  
**Goal:** Allow existing users to login and receive JWT + Refresh Token

```markdown
[ ] Add LoginAsync logic in AuthService
    - Find user by email
      → if null, return Result.Failure("Invalid credentials", 401)
    - Verify password via UserManager.CheckPasswordAsync
      → if failed, return Result.Failure("Invalid credentials", 401)
    - Generate JWT Token
    - Generate Refresh Token
    - Return Result.Success(new AuthResponse { ... })

    ⚠️ Return SAME error message for "user not found" and "wrong password"
    (prevents email enumeration attacks)

[ ] Add Login endpoint in AuthEndpoints.cs
    POST /api/auth/login
    - Accepts LoginRequest
    - Calls AuthService.LoginAsync()
    - Maps Result to HTTP response:
      result.IsSuccess → 200 + AuthResponse
      result.IsFailure → Results.Problem(result.Error, statusCode: result.StatusCode)
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
[ ] Create RefreshToken model in Core/Models/
    Properties:
    - Id (int)
    - Token (string)
    - ExpiresAt (DateTime)
    - CreatedAt (DateTime)
    - IsRevoked (bool)
    - UserId (string → FK to ApplicationUser)

[ ] Add RefreshTokens DbSet to AppDbContext

[ ] Add RefreshToken Migration
    Command: dotnet ef migrations add AddRefreshTokens --project ClinicManagementAPI.Core
                                                       --startup-project ClinicManagementAPI.Api

[ ] Apply Migration
    Command: dotnet ef database update --project ClinicManagementAPI.Core
                                       --startup-project ClinicManagementAPI.Api

[ ] Add RefreshTokenAsync logic in AuthService
    - Find token in Database
      → if null, return Result.Failure("Invalid token", 401)
    - Validate it is not expired and not revoked
      → if invalid, return Result.Failure("Token expired or revoked", 401)
    - Revoke old token
    - Generate new JWT Token
    - Generate new Refresh Token (with expiry from JwtSettings.RefreshTokenExpiryDays)
    - Return Result.Success(new AuthResponse { ... })

[ ] Add RevokeTokenAsync logic in AuthService
    - Find token in Database
      → if null, return Result.Failure("Invalid token", 400)
    - Mark it as revoked
    - Return Result.Success(true)

[ ] Add Refresh and Logout endpoints in AuthEndpoints.cs
    POST /api/auth/refresh
    - Accepts: { refreshToken: string }
    - Calls AuthService.RefreshTokenAsync()
    - Maps Result to HTTP response:
      result.IsSuccess → 200 + new AuthResponse
      result.IsFailure → Results.Problem(result.Error, statusCode: result.StatusCode)

    POST /api/auth/logout
    - Accepts: { refreshToken: string }
    - Requires valid JWT Token (protected endpoint)
    - Calls AuthService.RevokeTokenAsync()
    - Maps Result to HTTP response:
      result.IsSuccess → 204
      result.IsFailure → Results.Problem(result.Error, statusCode: result.StatusCode)
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

---

## Section 8 — Tests

**Expected Time: 60 minutes**  
**Goal:** 70%+ coverage on Auth logic — catch bugs before they reach production

```markdown
[ ] Create Unit Tests in Tests/Unit/AuthServiceTests.cs
    Test cases:
    - RegisterAsync_WithValidData_ReturnsSuccessResult
    - RegisterAsync_WithExistingEmail_ReturnsFailureResult
    - LoginAsync_WithValidCredentials_ReturnsSuccessResult
    - LoginAsync_WithWrongPassword_ReturnsFailureResult
    - LoginAsync_WithNonExistentEmail_ReturnsFailureResult (same error as wrong password)
    - RefreshTokenAsync_WithValidToken_ReturnsSuccessResult
    - RefreshTokenAsync_WithExpiredToken_ReturnsFailureResult
    - RevokeTokenAsync_WithValidToken_ReturnsSuccessResult

[ ] Create Integration Tests in Tests/Integration/AuthEndpointsTests.cs
    Test cases:
    - POST /api/auth/register → 201 with valid data
    - POST /api/auth/register → 400 with missing fields
    - POST /api/auth/register → 400 with invalid email format
    - POST /api/auth/register → 400 with short password (< 8 chars)
    - POST /api/auth/register → 400 with duplicate email
    - POST /api/auth/login    → 200 with valid credentials
    - POST /api/auth/login    → 401 with wrong password
    - POST /api/auth/login    → 401 with non-existent email (same error)
    - POST /api/auth/refresh  → 200 with valid refresh token
    - POST /api/auth/refresh  → 401 with expired token
    - POST /api/auth/logout   → 204 on success
    - POST /api/auth/logout   → 401 without JWT (unauthorized)

[ ] Run all tests and verify they pass
    Command: dotnet test --verbosity normal

[ ] Check coverage
    Command: dotnet test --collect:"XPlat Code Coverage"
```

---

## Section 9 — CI Update

**Expected Time: 15 minutes**  
**Goal:** CI pipeline now runs Auth tests automatically on every push

```markdown
[ ] Verify build.yml runs dotnet test (already configured in Sprint 1)

[ ] Add coverage report step to build.yml
    - name: Test with coverage
      run: dotnet test --collect:"XPlat Code Coverage" --verbosity normal

[ ] Push to GitHub and verify:
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
