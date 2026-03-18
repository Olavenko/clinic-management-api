# Clinic Management API

[![CI](https://github.com/Olavenko/clinic-management-api/actions/workflows/build.yml/badge.svg)](https://github.com/Olavenko/clinic-management-api/actions/workflows/build.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C%23](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-10.0-512BD4?logo=dotnet)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC292B?logo=microsoftsqlserver&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-181_Tests-512BD4?logo=dotnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Status: Complete](https://img.shields.io/badge/Status-Complete-success.svg)

A RESTful API for managing clinic appointments, patients, and doctors — built with ASP.NET Core Minimal API (.NET 10). Features JWT authentication with refresh tokens, role-based authorization, strict input validation, soft delete, appointment overlap detection, and 181 passing tests.

## Tech Stack

- **.NET 10** / ASP.NET Core Minimal API
- **Entity Framework Core** + SQL Server
- **ASP.NET Core Identity** + JWT Bearer Tokens + Refresh Token Rotation
- **Result Pattern** — business errors as return values, not exceptions
- **xUnit** — 181 tests (unit + integration) with Coverlet coverage
- **GitHub Actions CI** — build + test on every push
- **OpenAPI** + Scalar UI for interactive API docs

## Architecture

3-project layered architecture with clear dependency direction:

```
ClinicManagementAPI.Api        → Endpoints, Filters, Middleware, Program.cs
ClinicManagementAPI.Core       → Models, Services, Interfaces, Data, DTOs, Migrations
ClinicManagementAPI.Tests      → Unit + Integration tests (181 tests)
```

`Api → Core ← Tests` — Core has zero dependency on the web layer.

> **Visualizing the Architecture**: UML diagrams for each sprint (sequence, class, and use-case) are available in the `docs/uml/` directory as both PlantUML source and SVG exports. See [`docs/uml/Diagrams-Readme.md`](docs/uml/Diagrams-Readme.md) for details.

### Key Design Decisions

| Decision | Why |
|----------|-----|
| **Result Pattern** | Business errors (duplicate email, invalid status) returned as `Result<T>` — no exception-driven control flow |
| **Global Exception Handler** | Unexpected errors caught centrally, returned as ProblemDetails JSON. Stack traces never exposed |
| **Soft Delete** (Patient, Doctor) | EF Core Global Query Filters automatically exclude deleted records from all queries |
| **Status Lifecycle** (Appointment) | Scheduled → Completed or Cancelled. Terminal states are immutable — no soft delete needed |
| **Overlap Detection** | Formula-based time overlap check prevents double-booking for both patients and doctors |
| **Rate Limiting** | FixedWindowLimiter on Auth endpoints prevents brute force and enumeration attacks |
| **Central Package Management** | All NuGet versions in `Directory.Packages.props` — single source of truth |
| **User Secrets** | Connection string and JWT key never committed to source control |

## Role-Based Authorization

| Action | Admin | Receptionist | Patient |
|--------|:-----:|:------------:|:-------:|
| Register / Login | ✓ | ✓ | ✓ |
| View doctors | ✓ | ✓ | ✓ |
| Manage patients (CRUD) | ✓ | ✓ | — |
| Manage appointments | ✓ | ✓ | — |
| Manage doctors (CRUD) | ✓ | — | — |
| Delete patients / appointments | ✓ | — | — |
| Assign roles | ✓ | — | — |

All new registrations default to the **Patient** role.

## API Endpoints

### Auth
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/auth/register` | Public | Register new user |
| POST | `/api/auth/login` | Public | Login → JWT + Refresh Token |
| POST | `/api/auth/refresh` | Public | Refresh expired JWT |
| POST | `/api/auth/logout` | JWT | Revoke refresh token |

### Users
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| PUT | `/api/users/{id}/role` | Admin | Assign role to user |

### Patients
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/patients` | Admin, Receptionist | List with search + pagination |
| GET | `/api/patients/{id}` | Admin, Receptionist | Get by ID |
| POST | `/api/patients` | Admin, Receptionist | Create patient |
| PUT | `/api/patients/{id}` | Admin, Receptionist | Update patient |
| DELETE | `/api/patients/{id}` | Admin | Soft delete |

### Doctors
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/doctors` | Public | List with search + pagination |
| GET | `/api/doctors/available` | Public | Available doctors only |
| GET | `/api/doctors/{id}` | Public | Get by ID |
| POST | `/api/doctors` | Admin | Create doctor |
| PUT | `/api/doctors/{id}` | Admin | Update doctor |
| DELETE | `/api/doctors/{id}` | Admin | Soft delete |

### Appointments
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/appointments` | Admin, Receptionist | List with filters + pagination |
| GET | `/api/appointments/{id}` | Admin, Receptionist | Get by ID |
| GET | `/api/appointments/patient/{id}` | Admin, Receptionist | By patient |
| GET | `/api/appointments/doctor/{id}` | Admin, Receptionist | By doctor |
| POST | `/api/appointments` | Admin, Receptionist | Book appointment |
| PUT | `/api/appointments/{id}` | Admin, Receptionist | Reschedule (scheduled only) |
| PATCH | `/api/appointments/{id}/status` | Admin, Receptionist | Complete or cancel |
| DELETE | `/api/appointments/{id}` | Admin | Delete (cancelled only) |

### Utility
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/health` | Public | API + database connectivity check |

## Business Rules

- Appointments cannot be booked in the past
- Patients and doctors cannot have overlapping appointments
- Only **available** doctors can receive new appointments
- Only **scheduled** appointments can be updated or rescheduled
- Completed and cancelled appointments are **terminal states** — cannot be changed
- Only **cancelled** appointments can be deleted (audit trail preserved)
- Status transitions: `Scheduled → Completed` | `Scheduled → Cancelled` | all others rejected

## Testing

**181 tests** — all passing. Run with:

```bash
dotnet test --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Service Coverage

| Service | Line Coverage |
|---------|:------------:|
| AppointmentService | 95.2% |
| AuthService | 99.5% |
| PatientService | 99.4% |
| DoctorService | 100% |

Tests cover all four service modules including edge cases for overlap detection, status transitions, soft-delete interactions, and authentication flows.

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB, Express, or full instance)

### Setup

```bash
# Clone
git clone https://github.com/Olavenko/clinic-management-api.git
cd clinic-management-api

# Configure secrets (never committed)
dotnet user-secrets set "ConnectionStrings:ClinicDb" "Server=localhost;Database=ClinicDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ClinicManagementAPI.Api
dotnet user-secrets set "Jwt:Key" "your-secret-key-at-least-32-characters" --project ClinicManagementAPI.Api

# Apply migrations
dotnet ef database update --project ClinicManagementAPI.Core --startup-project ClinicManagementAPI.Api

# Run
dotnet run --project ClinicManagementAPI.Api

# Open Scalar UI at https://localhost:<port>/scalar/v1
```

> **Note**: Development mode automatically configures CORS `AllowAnyOrigin()` for easy local frontend testing.

## CI/CD

GitHub Actions runs **build + test** on every push to `main`/`develop` and on pull requests. Dependabot checks NuGet and Actions updates weekly.

## Development Roadmap

- [x] **Sprint 1** — Project Setup, CI Pipeline, Result Pattern, Global Error Handling, Health Check
- [x] **Sprint 2** — ASP.NET Core Identity, JWT Authentication, Refresh Tokens, Input Validation
- [x] **Sprint 3** — Patients CRUD, Role-Based Authorization, Assign Role, Search + Pagination
- [x] **Sprint 4** — Doctors CRUD, Public Endpoints, Admin Write Access, Soft Delete
- [x] **Sprint 5** — Appointments, Business Logic, Overlap Detection, Status Lifecycle, Filters
- [x] **Sprint 6** — OpenAPI/Scalar UI, Test Coverage Review, Documentation, Branching Strategy

## Documentation

The `docs/` directory contains comprehensive documentation for this project:

- [**Project Overview**](docs/PROJECT-OVERVIEW.md): High-level architecture, design decisions, and domain rules.
- [**Development Standards**](docs/DEVELOPMENT-STANDARDS.md): Coding guidelines, branching strategy, and naming conventions.
- [**Architecture Decision Records (ADR)**](docs/ADR/Index-ADR.md): A log of critical architectural choices made during development.
- [**UML Diagrams**](docs/uml/Diagrams-Readme.md): Sequence, class, and use-case diagrams for each sprint (PlantUML source + SVG exports).
- [**Releases**](docs/releases/): Release templates and version history tracking.
- [**Deferred Tasks**](docs/DeferredTasks/): Features and technical debt items deferred during sprints.
- [**Knowledge Base**](docs/KnowledgeBase/): General project documentation and findings.

## License

[MIT](LICENSE)
