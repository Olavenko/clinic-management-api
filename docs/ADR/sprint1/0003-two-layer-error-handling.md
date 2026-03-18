# ADR 0003: Two-Layer-Error-Handling-Strategy

- **Status**: Accepted
- **Date**: 2026-03-10
- **Owners**: Mohamed Ali
- **Related**: Sprint 1
- **Tags**: error-handling, middleware, result-pattern, exception-handler

---

## Context

An API encounters two fundamentally different kinds of errors:

1. **Expected errors** — invalid input, wrong password, duplicate email, booking conflict.
   These are part of normal business logic. The service knows exactly what went wrong and
   can describe it.
2. **Unexpected errors** — database connection lost, null reference, out-of-memory. These
   are genuinely exceptional. The service did not anticipate them.

Most APIs handle both with the same mechanism — either exceptions for everything, or
result types for everything. This project needed to decide: one mechanism or two?

## Issues Addressed

| #   | Severity | Category    | Summary                                                              | Location                         |
| --- | -------- | ----------- | -------------------------------------------------------------------- | -------------------------------- |
| S1  | High     | Security    | Stack traces must never leak to API consumers                        | Api/Middleware/                   |
| S2  | High     | Reliability | Unexpected exceptions must always produce a valid JSON response      | Api/Middleware/                   |
| S3  | Medium   | Design      | Expected business errors should not use exception control flow       | Core/Services/                   |

## Decision

Use **two separate layers**, each handling one category of error:

- **Layer 1 — `Result<T>`** for expected errors. Services return
  `Result<T>.Failure("message", statusCode)` when the operation fails for a known reason.
  Endpoints check `result.IsSuccess` and return the appropriate HTTP response. No
  exceptions are thrown.

- **Layer 2 — `GlobalExceptionHandler`** for unexpected errors. Implements
  `IExceptionHandler` and catches any exception that escapes the normal request pipeline.
  Logs the full exception (including stack trace) via `ILogger` for debugging, then returns
  a generic `ProblemDetails` response with status 500 and no internal details.

The two layers never overlap: if a service can describe the error, it uses Result. If
something truly unexpected happens, the exception bubbles up to GlobalExceptionHandler.

## Alternatives Considered

| Option                                      | Pros                                                                    | Cons                                                                                                               | Verdict  |
| ------------------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ | -------- |
| **Result\<T\> + GlobalExceptionHandler**    | Each mechanism handles what it's designed for; clear separation of intent; expected paths have no exception overhead | Two patterns to learn; developers must know which to use when                                                        | Adopted  |
| Exceptions only (try/catch everywhere)      | One pattern for everything; familiar to most developers                 | Business errors abuse exception semantics; stack trace allocation on every "wrong password"; catch blocks scattered across endpoints | Rejected |
| Result\<T\> only (no global handler)        | Consistent — every error flows through Result                           | Cannot catch truly unexpected exceptions (null ref, DB crash); unhandled exceptions produce default HTML error page or no response at all | Rejected |
| Custom middleware (not IExceptionHandler)    | Full control over the pipeline; works on older .NET versions            | Reinvents what IExceptionHandler already provides; must manually handle edge cases (response already started, content negotiation) | Rejected |

## Testing

- **Result layer**: Unit tests verify that services return `Result<T>.Failure()` with
  correct error messages and status codes for every expected failure path (e.g., duplicate
  email → 409, invalid credentials → 401, not found → 404).
- **GlobalExceptionHandler**: Integration tests can verify that if a service throws an
  unexpected exception, the API returns a 500 response with ProblemDetails JSON and no
  stack trace in the body.

## Consequences

- **Benefits**: Clean separation — developers always know where to look. Expected errors
  are fast (no stack trace allocation). Unexpected errors are safe (no information leakage).
  Logging captures full details for debugging while the client sees only a generic message.
- **Drawbacks**: New contributors must understand the convention: "use Result for business
  errors, let exceptions bubble for unexpected ones." This is documented in CLAUDE.md but
  requires discipline.
- **Trade-offs**: The two-layer approach adds a small amount of conceptual overhead compared
  to "just throw everywhere," but it pays off in correctness — expected errors are handled
  explicitly, and unexpected errors have a guaranteed safety net.

## References

- [Result.cs](../../ClinicManagementAPI.Core/Models/Result.cs) — Layer 1 implementation
- [GlobalExceptionHandler.cs](../../ClinicManagementAPI.Api/Middleware/GlobalExceptionHandler.cs) — Layer 2 implementation
- [ADR 0002](0002-custom-result-pattern.md) — the Result<T> design decision
- Sprint 1 Checklist, Section 8 (Result Pattern) and Section 10 (Global Error Handling)
