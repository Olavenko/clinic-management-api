# ADR 0002: Custom-Result-Pattern

- **Status**: Accepted
- **Date**: 2026-03-10
- **Owners**: Mohamed Ali
- **Related**: Sprint 1
- **Tags**: error-handling, result-pattern, service-layer

---

## Context

Every service method needs a way to communicate success or failure back to the endpoint
layer. The fundamental question: when a user provides a wrong password or tries to book
an appointment with a deleted doctor, how should the service report that?

This decision affects every service method signature in the project and determines how
endpoints translate business outcomes into HTTP responses.

## Issues Addressed

| #   | Severity | Category      | Summary                                                        | Location                  |
| --- | -------- | ------------- | -------------------------------------------------------------- | ------------------------- |
| S1  | High     | Architecture  | Services need a consistent way to express success and failure  | Core/Services/            |
| S2  | Medium   | API Design    | Endpoints need to map business errors to correct HTTP statuses | Api/Endpoints/            |
| S3  | Medium   | Testability   | Test assertions should be explicit about success vs failure    | Tests/Unit/               |

## Decision

Implement a **custom `Result<T>` class** in `Core/Models/Result.cs` with:

- `IsSuccess` (bool) — did the operation succeed?
- `Value` (T?) — the data on success
- `Error` (string?) — the error message on failure
- `StatusCode` (int) — the HTTP status code the endpoint should return

Two static factory methods:
- `Result<T>.Success(T value)` — creates a success result with status 200
- `Result<T>.Failure(string error, int statusCode = 400)` — creates a failure result

The constructor is private — callers must go through the factory methods, making it
impossible to create an ambiguous result that is both successful and has an error.

Every service method returns `Task<Result<T>>`. Endpoints check `result.IsSuccess` and
return the appropriate HTTP response.

## Alternatives Considered

| Option                              | Pros                                                                                        | Cons                                                                                                                    | Verdict  |
| ----------------------------------- | ------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- | -------- |
| **Custom Result\<T\> with StatusCode** | Tailored to this project; services control HTTP semantics; zero dependencies; simple to understand | Embedding HTTP status codes in Core couples business logic to HTTP concepts; must be maintained manually                 | Adopted  |
| Throw exceptions for all errors     | Familiar pattern; no wrapper types; natural try/catch flow                                   | "Wrong password" is not exceptional — abuses exception semantics; stack trace overhead for expected paths; harder to test expected failures | Rejected |
| FluentResults / ErrorOr library     | Battle-tested; richer error types (multiple errors, metadata); community support             | External dependency for a simple need; learning curve for reviewers; the API surface is larger than what this project uses | Rejected |
| Nullable returns (T?)               | No wrapper type at all; simplest possible signature                                         | No way to carry error messages or status codes; caller cannot distinguish "not found" from "unauthorized"; loses context | Rejected |
| Tuple (bool success, T? value, string? error) | No custom type needed                                                                       | Awkward to destructure everywhere; no compile-time safety — caller can ignore the bool; no place for status code        | Rejected |

## Testing

- Every unit test asserts on `result.IsSuccess`, `result.Value`, `result.Error`, and
  `result.StatusCode` explicitly — proving the Result carries the correct information.
- Example: `RegisterAsync_WithDuplicateEmail_ReturnsFailureResult` verifies that
  `IsSuccess` is false, `Error` contains a meaningful message, and `StatusCode` is 409.

## Consequences

- **Benefits**: Every service method communicates intent clearly. Endpoints are thin — they
  just map Result to HTTP. Tests read naturally: `Assert.True(result.IsSuccess)`. No
  exception overhead on expected paths.
- **Drawbacks**: HTTP status codes in Core mean the business layer is aware of HTTP
  semantics. In a project with multiple transport layers (gRPC, message queue), this would
  be a problem. For a REST-only API, it's a pragmatic shortcut.
- **Trade-offs**: Chose simplicity over purity. A status-code-free Result with a separate
  mapping layer in Api would be "cleaner" but would add a translation step in every
  endpoint for no real benefit in this project.

## References

- [Result.cs](../../ClinicManagementAPI.Core/Models/Result.cs) — the implementation
- [ADR 0003](0003-two-layer-error-handling.md) — how Result<T> works alongside GlobalExceptionHandler
- Sprint 1 Checklist, Section 8 — Result Pattern
