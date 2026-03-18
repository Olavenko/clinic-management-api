# ADR 0001: Layered-Architecture-Three-Project

- **Status**: Accepted
- **Date**: 2026-03-10
- **Owners**: Mohamed Ali
- **Related**: Sprint 1
- **Tags**: architecture, project-structure, separation-of-concerns

---

## Context

The project needs a clear structure from day one. As a solo-developer portfolio project,
the architecture must balance two competing goals: enough separation to demonstrate
professional practices, and enough simplicity to avoid over-engineering for a codebase
that one person maintains.

The core question: how many .NET projects should the solution contain, and what are the
dependency rules between them?

## Issues Addressed

| #   | Severity | Category        | Summary                                                | Location              |
| --- | -------- | --------------- | ------------------------------------------------------ | --------------------- |
| S1  | Medium   | Architecture    | Business logic must not depend on web framework        | Core project boundary |
| S2  | Medium   | Testability     | Services need to be testable without spinning up HTTP  | Tests project         |
| S3  | Low      | Maintainability | Clear ownership of code — where does each file live?   | Folder structure      |

## Decision

Adopt a **3-project solution**: `Api`, `Core`, and `Tests`.

- **ClinicManagementAPI.Api** — Web layer. Owns endpoints, middleware, filters, and
  `Program.cs`. Depends on Core. Knows about HTTP, ASP.NET Core, and request/response
  pipeline concerns.
- **ClinicManagementAPI.Core** — Business layer. Owns models, services, interfaces, DTOs,
  data access (`AppDbContext`), and migrations. Has zero references to the Api project or
  any ASP.NET Core web hosting package. This is enforced by the project reference graph.
- **ClinicManagementAPI.Tests** — References both Api and Core. Contains unit tests
  (targeting services directly) and integration tests (targeting endpoints via
  `WebApplicationFactory`).

Dependency rule: `Api → Core ← Tests`. Core never references Api.

## Alternatives Considered

| Option                             | Pros                                                                                | Cons                                                                                                                          | Verdict      |
| :--------------------------------- | :---------------------------------------------------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------- | :----------- |
| **3-project (Api, Core, Tests)**   | Clear separation without overhead; Core compiles independently; easy to navigate    | Data access (EF Core) lives in Core alongside business logic — no separate Infrastructure project                             | **Adopted**  |
| **Single project + folders**       | Simplest possible setup; no project references to manage                            | No compile-time enforcement of layer boundaries; services can accidentally reference HTTP types; looks amateur in a portfolio | **Rejected** |
| **4-project Clean Architecture**   | Strict separation — Domain has no EF Core dependency; Infrastructure owns DbContext | Extra indirection; interfaces in Domain mirroring EF Core patterns add boilerplate; overkill for solo developer               | **Rejected** |
| **5+ project (Contracts, Shared)** | Maximum modularity; each project has a single responsibility                        | Explosion of project references; adds cognitive overhead; no team to benefit from the isolation                               | **Rejected** |

## Testing

- Unit tests reference Core directly and instantiate services with InMemory DB — confirms Core compiles and runs without Api.
- Integration tests reference both Api and Core via `WebApplicationFactory<Program>` — confirms the full pipeline works end-to-end.
- Build verification: `dotnet build` succeeds with the 3-project reference graph, and Core has no `Microsoft.AspNetCore.App` framework reference.

## Consequences

- **Benefits**: Core is portable — it could be reused by a different host (console app, background worker) without changes. Layer violations are caught at compile time, not code review. The solution is small enough to navigate without tooling.
- **Drawbacks**: EF Core lives in Core, which means the "business layer" has an infrastructure dependency. In a larger team this would warrant a separate Infrastructure project, but here it avoids unnecessary abstraction.
- **Trade-offs**: Chose pragmatism over purity. The project demonstrates understanding of layered architecture without cargo-culting Clean Architecture patterns that add cost but no value at this scale.

## References

- [Directory.Build.props](../../Directory.Build.props) — shared build settings across all 3 projects
- [ClinicManagementAPI.slnx](../../ClinicManagementAPI.slnx) — solution file listing all projects
- Sprint 1 Checklist, Section 3 — Solution Structure
