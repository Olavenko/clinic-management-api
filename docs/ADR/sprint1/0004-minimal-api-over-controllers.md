# ADR 0004: Minimal-API-Over-Controllers

- **Status**: Accepted
- **Date**: 2026-03-11
- **Owners**: Mohamed Ali
- **Related**: Sprint 1
- **Tags**: api-style, minimal-api, endpoints, asp-net-core

---

## Context

ASP.NET Core offers two approaches for defining HTTP endpoints:

1. **Controllers** — classes inheriting `ControllerBase` with action methods decorated by
   `[HttpGet]`, `[HttpPost]`, etc. This has been the default approach since ASP.NET Core 1.0.
2. **Minimal API** — lambda or method-group handlers registered directly on
   `WebApplication` via `MapGet()`, `MapPost()`, etc. Introduced in .NET 6 and matured
   significantly in .NET 7+.

The project needed to choose one approach and commit to it, since mixing both creates
inconsistency in routing, DI patterns, and request handling.

## Issues Addressed

| #   | Severity | Category      | Summary                                                          | Location           |
| --- | -------- | ------------- | ---------------------------------------------------------------- | ------------------ |
| S1  | Medium   | Architecture  | Need a consistent pattern for all endpoint definitions           | Api/Endpoints/     |
| S2  | Low      | Performance   | Endpoint layer should add minimal overhead to the request path   | Api/Program.cs     |
| S3  | Low      | Simplicity    | Reduce ceremony — no base classes or attributes needed           | Api/Endpoints/     |

## Decision

Use **Minimal API** with the following conventions:

- Each entity gets a **static class** with a `MapXxxEndpoints(this WebApplication app)`
  extension method (e.g., `AuthEndpoints`, `PatientEndpoints`).
- Each class uses `MapGroup("/api/xxx")` to define a route prefix, then chains
  `.WithTags()`, `.RequireAuthorization()`, and `.RequireRateLimiting()` on the group.
- Individual handlers are **private static async methods** within the same class — keeping
  the endpoint definition and handler logic together.
- Services are injected as method parameters, not constructor dependencies.
- `Program.cs` calls `app.MapAuthEndpoints()`, `app.MapPatientEndpoints()`, etc.

## Alternatives Considered

| Option                                        | Pros                                                                                         | Cons                                                                                                                | Verdict  |
| --------------------------------------------- | -------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- | -------- |
| **Minimal API with static endpoint classes**  | Zero base class inheritance; explicit routing via MapGroup; lightweight request pipeline; aligns with modern .NET direction | Less familiar to developers coming from MVC; no built-in model binding conventions like `[FromBody]` attribute; community examples still skew toward controllers | Adopted  |
| Controller-based (ControllerBase)             | Mature ecosystem; widely documented; built-in model binding, filters, and content negotiation | Requires class inheritance; heavier request pipeline (action filters, model state); more ceremony per endpoint; moving away from Microsoft's investment direction | Rejected |
| Carter library (Minimal API wrapper)          | Module-based organization; built-in FluentValidation integration; familiar to some developers | External dependency for organization that can be achieved with extension methods; adds abstraction over an already simple API; FluentValidation conflicts with project's Data Annotations choice | Rejected |
| FastEndpoints library                         | Strongly typed request/response per endpoint; built-in validation; vertical slice architecture | Heavy abstraction layer; opinionated structure that conflicts with the project's own conventions; external dependency for what .NET provides natively | Rejected |

## Testing

- Integration tests use `WebApplicationFactory<Program>` and `HttpClient` to call endpoints
  directly — Minimal API endpoints are fully testable through the same integration testing
  pattern used for controllers.
- The static endpoint classes are verified to register correctly by the fact that all
  integration tests successfully hit their routes (e.g., `POST /api/auth/register`,
  `GET /api/patients`).

## Consequences

- **Benefits**: Endpoint files are concise — no inheritance, no attributes, just method
  registrations. `MapGroup()` centralizes route prefixes and shared middleware (auth, rate
  limiting, tags) in one place. The pattern scales linearly — adding a new entity means
  adding one new static class and one `app.MapXxxEndpoints()` call.
- **Drawbacks**: Developers familiar only with MVC controllers need to learn the Minimal API
  patterns. Some advanced scenarios (content negotiation, custom model binding) require
  more manual work than controllers provide out of the box.
- **Trade-offs**: Chose the lighter, more modern approach over the established one. The
  project doesn't need controller features like `[ApiController]` automatic model state
  validation (replaced by `ValidationFilter<T>`) or built-in `ProblemDetails` responses
  (handled by `Result<T>` and `GlobalExceptionHandler`).

## References

- [AuthEndpoints.cs](../../ClinicManagementAPI.Api/Endpoints/AuthEndpoints.cs) — example endpoint class
- [PatientEndpoints.cs](../../ClinicManagementAPI.Api/Endpoints/PatientEndpoints.cs) — example with CRUD + pagination
- [Program.cs](../../ClinicManagementAPI.Api/Program.cs) — endpoint registration
- [ValidationFilter.cs](../../ClinicManagementAPI.Api/Filters/ValidationFilter.cs) — replaces controller model state validation
