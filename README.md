# Clinic Management API

![CI](https://github.com/Olavenko/clinic-management-api/actions/workflows/build.yml/badge.svg)

A production-ready RESTful API built with ASP.NET Core Minimal API for managing clinic appointments, patients, and doctors.

## Tech Stack

- .NET 10 / ASP.NET Core Minimal API
- Entity Framework Core + SQL Server
- ASP.NET Core Identity + JWT Authentication
- Refresh Token with secure revocation
- Result Pattern for business error handling
- xUnit + GitHub Actions CI
- OpenAPI + Scalar UI

## Project Structure

```
ClinicManagementAPI/
├── ClinicManagementAPI.Api/       → Endpoints, Filters, Middleware, Program.cs
├── ClinicManagementAPI.Core/      → Models, Services, Interfaces, Data, DTOs
├── ClinicManagementAPI.Tests/     → Unit + Integration Tests
├── Directory.Build.props          → Shared build settings
├── Directory.Packages.props       → Central Package Management
└── .editorconfig                  → Code style rules
```

## Architecture Decisions

- 3-project Clean Architecture (Api → Core ← Tests)
- Result Pattern for expected errors (no exceptions for business logic)
- Global Error Handler for unexpected errors (DB crash, null ref)
- Central Package Management for consistent NuGet versions
- User Secrets for sensitive configuration (never committed)

## Role Permissions

| Action                  | Admin | Receptionist | Patient |
|-------------------------|:-----:|:------------:|:-------:|
| Register / Login        |  Yes  |     Yes      |   Yes   |
| View doctors (public)   |  Yes  |     Yes      |   Yes   |
| Manage patients (CRUD)  |  Yes  |     Yes      |   No    |
| Manage appointments     |  Yes  |     Yes      |   No    |
| Manage doctors (CRUD)   |  Yes  |     No       |   No    |
| Delete patients         |  Yes  |     No       |   No    |
| Delete appointments     |  Yes  |     No       |   No    |
| Assign roles            |  Yes  |     No       |   No    |

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
| PUT    | /api/patients/{id}     | Admin, Recep.    | Update patient       |
| DELETE | /api/patients/{id}     | Admin            | Soft delete patient  |

### Doctors

| Method | Endpoint                 | Access   | Description              |
|--------|--------------------------|----------|--------------------------|
| GET    | /api/doctors             | Public   | List + search + page     |
| GET    | /api/doctors/available   | Public   | Available doctors only   |
| GET    | /api/doctors/{id}        | Public   | Get by ID                |
| POST   | /api/doctors             | Admin    | Create doctor            |
| PUT    | /api/doctors/{id}        | Admin    | Update doctor            |
| DELETE | /api/doctors/{id}        | Admin    | Soft delete doctor       |

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
- Status transitions: Scheduled → Completed | Scheduled → Cancelled | others rejected

## How to Run

```bash
# 1. Clone the repository
git clone https://github.com/Olavenko/clinic-management-api.git
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

**181 tests** (unit + integration) covering Auth, Patient, Doctor, and Appointment modules — including overlap detection, status transitions, and soft-delete interaction.

## Architecture and Diagrams

Comprehensive UML documentation is maintained in `docs/uml/`, created with PlantUML and a custom dark theme:

- **Sprint 1**: Layered Architecture and CI Pipeline Component Diagram
- **Sprint 2**: Authentication Sequence, Use Case, and Class Diagrams
- **Sprint 3**: Patients CRUD Component, Sequence, ERD, and Class Diagrams
- **Sprint 4**: Doctors CRUD Component, Sequence, ERD, and Class Diagrams
- **Sprint 5**: Appointments + Business Logic Component, Sequence, ERD, and Class Diagrams

SVG exports are available in each sprint's `exports/` subfolder. See `docs/uml/Diagrams-Readme.md` for generation instructions.

## License

[MIT](LICENSE)
