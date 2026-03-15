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
├── ClinicManagementAPI.Api/          # API layer (depends on Core)
│   ├── Endpoints/                    # Static classes with MapXxxEndpoints()
│   ├── Filters/                      # ValidationFilter<T> (Data Annotations)
│   ├── Middleware/                   # GlobalExceptionHandler (IExceptionHandler)
│   └── Program.cs                    # App entry point & service registration
│
├── ClinicManagementAPI.Core/         # Core/business layer (knows nothing about Web)
│   ├── Data/                         # AppDbContext, DatabaseSeeder
│   ├── DTOs/                         # All DTOs (Auth/, Patients/, Doctors/, Appointments/)
│   ├── Interfaces/                   # Service contracts (IAuthService, IPatientService, IDoctorService, IAppointmentService)
│   ├── Models/                       # Entities, enums, Result<T>, JwtSettings
│   ├── Services/                     # AuthService, PatientService, DoctorService, AppointmentService
│   └── Migrations/                   # EF Core migrations
│
├── ClinicManagementAPI.Tests/        # Test project
│   ├── Unit/                         # Unit tests
│   └── Integration/                  # Integration tests
│
├── Roadmap/                          # Sprint checklists & planning docs
├── docs/                             # Architecture & Documentation
│   └── uml/                          # PlantUML diagrams (.puml) & SVG exports
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

## Architecture & Diagrams

Comprehensive UML documentation is maintained in the `docs/uml/` directory, created with PlantUML and a premium dark theme.

- **Sprint 1**: Layered Architecture & CI Pipeline Component Diagram (`docs/uml/sprint1/`)
- **Sprint 2**: Authentication Sequence, Use Case, and Class Diagrams (`docs/uml/sprint2/`)
- **Sprint 3**: Patients CRUD Component, Sequence, ERD, and Class Diagrams (`docs/uml/sprint3/`)
- **Sprint 4**: Doctors CRUD Component, Sequence, ERD, and Class Diagrams (`docs/uml/sprint4/`)
- **Sprint 5**: Appointments + Business Logic Component, Sequence, ERD, and Class Diagrams (`docs/uml/sprint5/`)

High-fidelity `.svg` vector images with built-in OpenIconic sprites are exported into the respective `exports/` subfolders for easy viewing. Learn how to generate or modify these diagrams by reading `docs/uml/Diagrams-Readme.md`.

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

## Roadmap & Progress

- [x] **Sprint 1** — Project Setup + CI (Build) *(Completed: 2026-03-10)*
- [x] **Sprint 2** — Authentication (JWT + Refresh Token + Roles Setup) *(Completed: 2026-03-14)*
- [x] **Sprint 3** — Patients CRUD + Role-based Authorization + Assign Role *(Completed: 2026-03-14)*
- [x] **Sprint 4** — Doctors CRUD + Public GET + Admin Write + Soft Delete *(Completed: 2026-03-15)*
- [x] **Sprint 5** — Appointments + Business Logic (Overlap Detection, Status Lifecycle) *(Completed: 2026-03-15)*
- [ ] **Sprint 6** — Polish + Documentation + Coverage Review *(Planned)*

## API Endpoints

| Method   | Endpoint                          | Description                                      |
|----------|-----------------------------------|--------------------------------------------------|
| `GET`    | `/health`                         | Health check (API + database)                    |
| `GET`    | `/openapi/v1.json`                | OpenAPI specification (dev only)                 |
| `POST`   | `/api/auth/register`              | Register a new user                              |
| `POST`   | `/api/auth/login`                 | Login and receive JWT & Refresh Token            |
| `POST`   | `/api/auth/refresh`               | Refresh an expired JWT using refresh token       |
| `POST`   | `/api/auth/logout`                | Logout and revoke refresh token                  |
| `PUT`    | `/api/users/{id}/role`            | Assign a role to a user                          |
| `GET`    | `/api/patients`                   | Get a paginated list of patients                 |
| `GET`    | `/api/patients/{id}`              | Get a specific patient by ID                     |
| `POST`   | `/api/patients`                   | Add a new patient                                |
| `PUT`    | `/api/patients/{id}`              | Update an existing patient                       |
| `DELETE` | `/api/patients/{id}`              | Soft delete a patient                            |
| `GET`    | `/api/doctors`                    | Get a paginated list of doctors (public)         |
| `GET`    | `/api/doctors/available`          | Get available doctors only (public)              |
| `GET`    | `/api/doctors/{id}`               | Get a specific doctor by ID (public)             |
| `POST`   | `/api/doctors`                    | Add a new doctor (Admin only)                    |
| `PUT`    | `/api/doctors/{id}`               | Update an existing doctor (Admin only)           |
| `DELETE` | `/api/doctors/{id}`               | Soft delete a doctor (Admin only)                |
| `GET`    | `/api/appointments`               | Get paginated appointments (filtered)            |
| `GET`    | `/api/appointments/{id}`          | Get a specific appointment by ID                 |
| `GET`    | `/api/appointments/patient/{id}`  | Get appointments by patient                      |
| `GET`    | `/api/appointments/doctor/{id}`   | Get appointments by doctor                       |
| `POST`   | `/api/appointments`               | Book a new appointment                           |
| `PUT`    | `/api/appointments/{id}`          | Update a scheduled appointment                   |
| `PATCH`  | `/api/appointments/{id}/status`   | Change appointment status                        |
| `DELETE` | `/api/appointments/{id}`          | Hard delete a cancelled appointment (Admin only) |

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

## Authorization

| Endpoint Group                          | Public | Patient | Receptionist | Admin |
|-----------------------------------------|--------|---------|--------------|-------|
| `GET /api/doctors*`                     | Yes    | Yes     | Yes          | Yes   |
| `POST/PUT/DELETE /api/doctors`          | No     | No      | No           | Yes   |
| `GET /api/patients*`                    | No     | No      | Yes          | Yes   |
| `POST/PUT /api/patients`                | No     | No      | Yes          | Yes   |
| `DELETE /api/patients`                  | No     | No      | No           | Yes   |
| `GET/POST/PUT/PATCH /api/appointments*` | No     | No      | Yes          | Yes   |
| `DELETE /api/appointments`              | No     | No      | No           | Yes   |
| `PUT /api/users/{id}/role`              | No     | No      | No           | Yes   |

All new registrations default to the **Patient** role.

## Testing

```bash
dotnet test
```

**181 tests** (unit + integration) covering Auth, Patient, Doctor, and Appointment modules — including overlap detection, status transitions, and soft-delete interaction tests.

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
