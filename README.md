# 🏥 Clinic Management API

A RESTful API built with **ASP.NET Core Minimal API** for managing clinic appointments, patients, and doctors.

---

## Tech Stack

| Category           | Technology                                 |
|--------------------|--------------------------------------------|
| **Framework**      | .NET 10 / ASP.NET Core Minimal API         |
| **Database**       | Entity Framework Core + SQL Server         |
| **Authentication** | JWT Bearer Tokens + ASP.NET Identity       |
| **API Docs**       | OpenAPI (Scalar)                           |
| **Testing**        | xUnit + Coverlet (code coverage)           |
| **CI/CD**          | GitHub Actions                             |

## Project Structure

```markdown
ClinicManagementAPI/
├── ClinicManagementAPI.Api/          # API layer (endpoints, middleware, DTOs)
│   ├── DTOs/                         # Data Transfer Objects
│   ├── Endpoints/                    # Minimal API endpoint definitions
│   ├── Middleware/                   # Global exception handling
│   └── Program.cs                    # App entry point & service registration
│
├── ClinicManagementAPI.Core/         # Core/business layer
│   ├── Data/                         # EF Core DbContext
│   ├── Interfaces/                   # Service contracts
│   ├── Models/                       # Domain entities
│   └── Services/                     # Business logic
│
├── ClinicManagementAPI.Tests/        # Test project
│   ├── Unit/                         # Unit tests
│   └── Integration/                  # Integration tests
│
├── Roadmap/                          # Sprint checklists & planning docs
├── docs/                             # Additional documentation
├── .github/
│   ├── workflows/build.yml           # CI pipeline
│   └── dependabot.yml                # Automated dependency updates
├── .editorconfig                     # Code style & naming conventions
├── .gitattributes                    # Line ending normalization
├── .gitignore                        # Files excluded from source control
├── Directory.Build.props             # Shared build settings (TFM, nullable, etc.)
├── Directory.Packages.props          # Central Package Management
└── ClinicManagementAPI.slnx          # Solution file
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/) (LocalDB, Express, or full instance)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Olavenko/clinic-management-api.git
cd clinic-management-api
```

### 2. Configure User Secrets

The connection string and JWT key are stored securely via [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — they are never committed to source control.

```bash
cd ClinicManagementAPI.Api

dotnet user-secrets set "ConnectionStrings:ClinicDb" "Server=(localdb)\mssqllocaldb;Database=ClinicDb;Trusted_Connection=true;"
dotnet user-secrets set "Jwt:Key" "your-secret-key-at-least-32-characters-long"
```

### 3. Apply database migrations

```bash
dotnet ef database update --project ClinicManagementAPI.Core --startup-project ClinicManagementAPI.Api
```

### 4. Run the API

```bash
dotnet run --project ClinicManagementAPI.Api
```

The API will start at `https://localhost:5001` (or the port configured in `launchSettings.json`).

## API Endpoints

| Method | Endpoint           | Description                      |
|--------|--------------------|----------------------------------|
| `GET`  | `/health`          | Health check (API + database)    |
| `GET`  | `/openapi/v1.json` | OpenAPI specification (dev only) |

> More endpoints will be added in upcoming sprints.

## Health Checks

The API exposes a `/health` endpoint that verifies both the API and database connectivity:

```markdown
GET /health → "Healthy"    (when DB is connected)
GET /health → "Unhealthy"  (when DB is down)
```

## Error Handling

All unhandled exceptions are caught by the `GlobalExceptionHandler` middleware and returned as a standardized JSON [Problem Details](https://datatracker.ietf.org/doc/html/rfc9457) response:

```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "Something went wrong. Please try again later."
}
```

Stack traces are **never** exposed to clients.

## Testing

```bash
dotnet test
```

Tests are automatically run on every push and pull request via GitHub Actions CI.

## CI/CD

The GitHub Actions workflow (`.github/workflows/build.yml`) runs on:

- **Push** to `main` or `develop`
- **Pull requests** targeting `main`

Pipeline steps: Restore → Build → Test

### Dependabot

Automated dependency updates are configured via `.github/dependabot.yml`:

- **NuGet packages** — checked weekly (Mondays)
- **GitHub Actions** — checked weekly (Mondays)

### Branch Protection

The `main` branch is protected with the following rules:

- All changes must go through a **Pull Request**
- CI status checks must **pass** before merging

## Build Configuration

This project uses modern .NET practices:

- **Central Package Management** — all NuGet versions defined in `Directory.Packages.props`
- **Shared Build Props** — common settings in `Directory.Build.props`:
  - Target Framework: `net10.0`
  - Nullable reference types: enabled
  - Implicit usings: enabled
  - Warnings as Errors: enforced
  - Code style enforcement in build: enabled
  - Analysis level: latest
- **File-scoped namespaces** and **primary constructors** — enforced via `.editorconfig`
- **Line ending normalization** — ensured via `.gitattributes`

## License

This project is for educational and portfolio purposes.
