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
