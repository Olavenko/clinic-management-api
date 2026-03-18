# ADR 0005: DTOs-In-Core-Instead-Of-Api

- **Status**: Accepted
- **Date**: 2026-03-11
- **Owners**: Mohamed Ali
- **Related**: Sprint 1
- **Tags**: architecture, dtos, project-structure, deviation-from-plan

---

## Context

The Sprint 1 checklist placed DTOs in `Api/DTOs/` — the web layer. This is a common
convention: request/response objects live near the endpoints that use them.

However, during implementation it became clear that services in Core need to accept and
return DTOs. For example, `IAuthService.RegisterAsync(RegisterRequest request)` takes a
`RegisterRequest` and returns `Result<AuthResponse>`. If these DTOs live in Api, then
Core must reference Api — which violates the dependency rule (`Api → Core`, never the
reverse).

The project had to choose: move DTOs to Core, or introduce an indirection layer to avoid
the circular dependency.

## Issues Addressed

| #   | Severity | Category       | Summary                                                            | Location                |
| --- | -------- | -------------- | ------------------------------------------------------------------ | ----------------------- |
| S1  | High     | Architecture   | Services in Core cannot reference types defined in Api             | Core/Services/          |
| S2  | Medium   | Dependency     | Circular reference between Api and Core would break the build      | Project references      |
| S3  | Low      | Plan Deviation | Original Sprint 1 plan placed DTOs in Api/DTOs/                    | Sprint 1 Checklist §7   |

## Decision

Move all DTOs to **`Core/DTOs/`**, organized by feature subdirectory:

```
Core/DTOs/
├── Auth/
│   ├── RegisterRequest.cs
│   ├── LoginRequest.cs
│   ├── AuthResponse.cs
│   ├── RefreshTokenRequest.cs
│   └── AssignRoleRequest.cs
├── Patients/
│   ├── CreatePatientRequest.cs
│   ├── UpdatePatientRequest.cs
│   └── PatientResponse.cs
├── Doctors/
├── Appointments/
├── PaginationRequest.cs
└── PagedResponse.cs
```

Service interfaces in Core reference these DTOs directly. Api references them through its
existing project reference to Core. No `Api/DTOs/` folder exists.

## Alternatives Considered

| Option                                  | Pros                                                                                  | Cons                                                                                                                                           | Verdict  |
| --------------------------------------- | ------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- | -------- |
| **DTOs in Core/DTOs/**                  | Services reference DTOs directly; no circular dependency; single source of truth for all data contracts | Core now contains HTTP-facing types (request/response objects); "pure" domain purists would argue these belong in the web layer                 | Adopted  |
| DTOs in Api/DTOs/ (original plan)       | DTOs stay near endpoints; Core stays "pure" with only domain types                    | Services cannot reference request/response types; would need separate domain models + mapping in every service method; doubles the type count   | Rejected |
| Separate Contracts project (Api.Contracts) | Maximum separation; both Api and Core reference a shared Contracts project            | Fourth project for a 3-project solution; adds indirection for types that are only used between two projects; over-engineered for this scale     | Rejected |
| Internal domain types + mapping in Api  | Core uses its own types; Api maps DTOs ↔ domain types at the endpoint boundary        | Every endpoint needs manual mapping in both directions; doubles the code for every request/response; mapping bugs become a new failure category | Rejected |

## Testing

- Unit tests in Tests/Unit/ import DTOs from `ClinicManagementAPI.Core.DTOs.*` to construct
  requests and assert on responses — confirming DTOs are accessible without referencing Api.
- Integration tests import the same DTOs for serializing HTTP request bodies and
  deserializing responses.
- No `Api/DTOs/` folder exists in the solution — verified by folder structure inspection.

## Consequences

- **Benefits**: Services have direct access to the types they need. No mapping layer between
  "API DTOs" and "service DTOs." One set of validation attributes on DTOs serves both the
  endpoint filter and service layer. Adding a new feature means creating DTOs once in Core.
- **Drawbacks**: Core is no longer a "pure" business layer — it contains HTTP-facing data
  contracts. If the project ever needed a non-HTTP interface (CLI, message queue), the
  request/response DTOs would be HTTP-specific baggage in Core.
- **Trade-offs**: This was a deliberate deviation from the Sprint 1 plan. The original plan
  didn't account for services needing to reference DTOs. Moving them to Core was the
  simplest fix that preserved the one-way dependency rule without adding a fourth project.

## References

- [Core/DTOs/](../../ClinicManagementAPI.Core/DTOs/) — all DTOs organized by feature
- [IAuthService.cs](../../ClinicManagementAPI.Core/Interfaces/IAuthService.cs) — service interface referencing DTOs from Core
- [ADR 0001](0001-layered-architecture.md) — the 3-project architecture this decision supports
- Sprint 1 Checklist, Section 7 — original folder structure plan
