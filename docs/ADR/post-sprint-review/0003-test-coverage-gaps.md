# ADR 0003: Test-Coverage-Gaps-Remediation

- **Status**: Accepted
- **Date**: 2026-03-20
- **Owners**: Mohamed Ali
- **Related**: Post-Sprint 6 — Test Coverage Session (follows ADR 0002)
- **Tags**: testing, coverage, authorization, pagination, security, token-rotation

---

## Context

After completing the codebase review (ADR 0002), a test coverage audit identified 10 gaps across critical, high, and medium priorities. Several gaps corresponded directly to fixes applied in ADR 0002 that lacked test verification (B1 token cleanup, B2 same-day past-time). Others covered missing authorization paths, edge-case inputs, and token security scenarios.

## Issues Addressed

| #   | Priority | Category      | Summary                                                    | Test File(s)                                      |
| --- | -------- | ------------- | ---------------------------------------------------------- | ------------------------------------------------- |
| T1  | Critical | Bug Coverage  | Same-day past-time appointment creation (B2 fix)           | AppointmentServiceTests.cs                        |
| T2  | Critical | Authorization | Receptionist role on Appointment CRUD (Create/Update/Status)| AppointmentEndpointsTests.cs                      |
| T3  | Critical | Authorization | Non-Admin cannot assign roles (Patient, Doctor tokens)     | AuthEndpointsTests.cs                             |
| T4  | High     | Validation    | Invalid/out-of-range AppointmentStatus values              | AppointmentServiceTests.cs                        |
| T5  | High     | Edge Case     | Pagination boundaries: page=0, pageSize=0, negative, >50  | PatientServiceTests.cs, AppointmentServiceTests.cs|
| T6  | High     | Security      | RefreshToken rotation — reuse detection after second refresh| AuthServiceTests.cs                               |
| T7  | High     | Bug Coverage  | Token cleanup persistence (B1 fix verification)            | AuthServiceTests.cs                               |
| S2  | High     | Authorization | Integration test for role-based access on AssignRole       | AuthEndpointsTests.cs (combined with T3)          |
| T8  | Medium   | Data Integrity| Case-insensitive email uniqueness                          | PatientServiceTests.cs                            |
| T9  | Medium   | Security      | Search with special characters / SQL-like strings          | PatientServiceTests.cs                            |
| T10 | Medium   | Edge Case     | Empty dataset pagination (zero records)                    | PatientServiceTests.cs, AppointmentServiceTests.cs|

## Decision

### Tests Added (20 new tests, 182 → 202 total)

**T1 — Same-day past-time (Critical)**
- `CreateAsync_WithSameDayPastTime_ReturnsFailureResult` — verifies B2 fix rejects today's date with a past time
- `UpdateAsync_WithSameDayPastTime_ReturnsFailureResult` — same validation on reschedule
- **Code fix required**: B2 fix was documented in ADR 0002 but not implemented. Applied same-day time validation in both `CreateAsync` and `UpdateAsync` of `AppointmentService`.

**T2 — Receptionist Appointment CRUD (Critical)**
- `CreateAppointment_WithReceptionistToken_Returns201` — Receptionist can create appointments
- `UpdateAppointment_WithReceptionistToken_Returns200` — Receptionist can update appointments
- `UpdateStatus_WithReceptionistToken_Returns200` — Receptionist can change appointment status
- Complements existing tests for Receptionist GET (200) and DELETE (403).

**T3/S2 — AssignRole Authorization (Critical)**
- `AssignRole_WithPatientToken_Returns403` — Patient role cannot assign roles
- `AssignRole_WithDoctorToken_Returns403` — Doctor role cannot assign roles
- Complements existing `AssignRole_WithReceptionistToken_Returns403`.

**T4 — Invalid Status Values (High)**
- `UpdateStatusAsync_WithInvalidStatusValue_ReturnsFailureResult` — out-of-range enum (99)
- `UpdateStatusAsync_ScheduledToScheduled_ReturnsFailureResult` — no-op transition rejected

**T5 — Pagination Boundaries (High)**
- `GetAllAsync_WithZeroPage_ClampsToPage1` — page=0 clamped to 1 (Appointments)
- `GetAllAsync_WithNegativePageSize_ClampsToDefault` — pageSize=-1 clamped to 10 (Appointments)
- `GetAllAsync_WithPageZero_ClampsToPage1` — page=0 clamped to 1 (Patients)
- `GetAllAsync_WithNegativePageSize_ClampsToDefault` — pageSize=-5 clamped to 10 (Patients)
- `GetAllAsync_WithOversizedPageSize_ClampsTo50` — pageSize=100 clamped to 50 (Patients)

**T6 — Token Rotation Reuse Detection (High)**
- `RefreshTokenAsync_ReuseAfterRotation_RevokesAllTokens` — uses token1 after rotation to token2; verifies reuse detection triggers and token2 is also revoked.

**T7 — Token Cleanup Persistence (High)**
- `RefreshTokenAsync_CleansUpExpiredTokens` — seeds an old expired token, triggers refresh, verifies the expired token is removed from DB (B1 fix).

**T8 — Case-insensitive Email (Medium)**
- `CreateAsync_WithCaseInsensitiveDuplicateEmail_ReturnsFailureResult` — "TEST@CLINIC.COM" rejected when "test@clinic.com" exists.
- **Code fix required**: Email comparisons in `PatientService` and `DoctorService` (Create + Update) used `==` which is case-sensitive in InMemory DB. Changed to `.ToLower()` comparison. Also fixed the early-exit `!=` guard in `UpdateAsync` to use `StringComparison.OrdinalIgnoreCase`.

**T9 — Special Characters in Search (Medium)**
- `GetAllAsync_WithSpecialCharactersInSearch_ReturnsEmptyResults` — SQL injection attempt string
- `GetAllAsync_WithSqlWildcardsInSearch_ReturnsEmptyResults` — SQL wildcards (`___%`)

**T10 — Empty Dataset Pagination (Medium)**
- `GetAllAsync_WithEmptyDataset_ReturnsEmptyPagedResponse` — Appointments with no data
- `GetAllAsync_WithEmptyDatabase_ReturnsEmptyPagedResponse` — Patients with no data

### Code Fixes Applied

| Fix | Location | Description |
| --- | -------- | ----------- |
| B2 (missing) | AppointmentService.cs CreateAsync + UpdateAsync | Added same-day past-time validation: if date == today, time must be in the future |
| T8 | PatientService.cs CreateAsync + UpdateAsync | Email comparison changed to `.ToLower()` for case-insensitive uniqueness |
| T8 | DoctorService.cs CreateAsync + UpdateAsync | Same case-insensitive email fix |

## Alternatives Considered

| Option | Pros | Cons | Verdict |
| --- | --- | --- | --- |
| Test T8 with SQL Server only | Matches production behavior | Cannot run in CI without SQL Server | Rejected |
| Fix email comparison in code | Works across all DB providers | Slight performance overhead from ToLower | Adopted |
| T5: Reject invalid pagination with 400 | Strict validation | Breaking change; PaginationRequest already clamps | Rejected |
| T5: Test clamping behavior | Verifies defensive design | Doesn't catch truly invalid input at HTTP layer | Adopted |

## Testing

- All 202 tests pass (182 existing + 20 new)
- 0 warnings, 0 errors
- Coverage areas expanded: authorization paths, pagination edge cases, token security, email uniqueness, empty datasets, input sanitization

## Consequences

- **Benefits**: All fixes from ADR 0002 are now verified by automated tests. Authorization coverage is comprehensive across all roles. Edge cases (empty data, boundary pagination, special characters) are hardened. Token rotation reuse detection is proven end-to-end.
- **Drawbacks**: None significant. Test suite runtime increased marginally (~0.5s).
- **Technical Debt Resolved**: S2 from ADR 0002 (integration test for AssignRole authorization) is now complete. B2 fix that was documented but missing is now implemented and tested.

## References

- [ADR 0002: Post-Sprint Codebase Review](0002-post-sprint-codebase-review.md)
- [ADR Template](../ADR-Template.md)
- Test Backlog: 10 items (T1–T10, S2) — all resolved
