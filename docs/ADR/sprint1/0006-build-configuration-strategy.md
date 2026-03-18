# ADR 0006: Build-Configuration-Strategy

- **Status**: Accepted
- **Date**: 2026-03-11
- **Owners**: Mohamed Ali
- **Related**: Sprint 1
- **Tags**: build, cpm, msbuild, code-quality, directory-build-props

---

## Context

A 3-project solution needs consistent build settings and package versions across all
projects. Without centralization, each `.csproj` file independently declares its target
framework, nullable context, warning behavior, and NuGet package versions. This creates
two problems:

1. **Drift** — one project targets net10.0 while another still says net9.0, or one project
   uses EF Core 10.0.3 while another uses 10.0.5.
2. **Silent quality erosion** — warnings pile up because nothing forces them to be fixed,
   and code style violations are only caught when a developer happens to notice them.

The project needed to decide how aggressively to enforce consistency and quality at build
time.

## Issues Addressed

| #   | Severity | Category     | Summary                                                         | Location                    |
| --- | -------- | ------------ | --------------------------------------------------------------- | --------------------------- |
| S1  | Medium   | Consistency  | Build settings repeated across 3 .csproj files can diverge      | *.csproj files              |
| S2  | Medium   | Quality      | Warnings ignored today become bugs tomorrow                     | All source files            |
| S3  | Medium   | Dependency   | Package version mismatches between projects cause runtime errors | Directory.Packages.props    |
| S4  | Low      | Code Style   | Style violations only caught in code review, not in build       | .editorconfig + build       |

## Decision

Adopt two complementary MSBuild files at the solution root:

### Directory.Build.props — Shared Build Settings

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>
</Project>
```

Key enforcement choices:
- **TreatWarningsAsErrors** — no warning survives the build. Every nullable warning,
  unused variable, or missing await is a compile error.
- **EnforceCodeStyleInBuild** — `.editorconfig` rules are enforced by the compiler, not
  just the IDE. Style violations (wrong namespace format, incorrect naming) break CI.
- **AnalysisLevel: latest** — enables the newest set of .NET code analyzers automatically.

Individual `.csproj` files only contain project-specific settings (SDK type, package
references, project references). No `TargetFramework`, `Nullable`, or warning settings
are repeated.

### Directory.Packages.props — Central Package Management (CPM)

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  <MicrosoftExtensionsVersion>10.0.5</MicrosoftExtensionsVersion>
</PropertyGroup>
```

All NuGet package versions are declared once in this file. Individual `.csproj` files
use `<PackageReference Include="..." />` without a `Version` attribute — CPM supplies it.

A custom MSBuild property `MicrosoftExtensionsVersion` pins all Microsoft packages (EF
Core, Identity, Authentication, Health Checks, Testing) to the same version, ensuring
they stay in lockstep. Third-party packages (Scalar, xUnit, Coverlet) have their own
explicit versions.

## Alternatives Considered

| Option                                                 | Pros                                                                            | Cons                                                                                                              | Verdict  |
| ------------------------------------------------------ | ------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- | -------- |
| **Directory.Build.props + CPM with version property**  | Single source of truth for build settings and package versions; CI catches style and version drift; zero repetition across projects | Strict — any warning or style violation blocks the build; new developers may find it frustrating initially          | Adopted  |
| Per-project settings (no Directory.Build.props)        | Each project is self-contained; no implicit MSBuild imports to understand        | Settings drift between projects; easy to forget TreatWarningsAsErrors in one project; TargetFramework can diverge  | Rejected |
| Per-project package versions (no CPM)                  | Simpler mental model — version is right next to the package reference            | Version conflicts between Api and Tests; manual effort to keep all projects on the same EF Core version            | Rejected |
| Warnings as warnings (no TreatWarningsAsErrors)       | Less friction during development; warnings don't block builds                   | Warnings accumulate until they're ignored; nullable violations hide real bugs; CI gives false green                 | Rejected |
| IDE-only style enforcement (no EnforceCodeStyleInBuild) | Developers see style suggestions in IDE without build failures                  | CI doesn't catch style violations; code style depends on each developer's IDE settings and discipline              | Rejected |
| Paket (alternative package manager)                    | Dependency groups; stricter version resolution; popular in F# ecosystem         | Non-standard for C# projects; adds tooling dependency; reviewers may not recognize the configuration               | Rejected |

## Testing

- `dotnet build` is run as part of CI (GitHub Actions). If any project has a warning or
  style violation, the build fails and the pipeline goes red.
- Package version consistency is verified implicitly — if two projects reference the same
  package with different versions, CPM throws a build error.
- The `.editorconfig` naming rules (e.g., `_camelCase` for private fields, `IXxx` for
  interfaces) are enforced at build time, not just in the IDE.

## Consequences

- **Benefits**: The build is the single source of truth for quality. No warnings slip
  through. Package versions cannot diverge. Code style is consistent without relying on
  manual review. New projects added to the solution automatically inherit all settings.
- **Drawbacks**: The strictness can slow down prototyping — you cannot leave a `TODO`
  warning or unused variable while iterating. Every build must be clean. This is
  intentional for a portfolio project but might be relaxed in a rapid-prototyping context.
- **Trade-offs**: Chose maximum strictness because this is a portfolio project where code
  quality signals matter. In a team setting with rapid iteration needs, the
  `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` combination might need a grace
  period or suppression mechanism during active development.

## References

- [Directory.Build.props](../../Directory.Build.props) — shared build settings
- [Directory.Packages.props](../../Directory.Packages.props) — central package versions
- [.editorconfig](../../.editorconfig) — code style rules enforced by EnforceCodeStyleInBuild
- Sprint 1 Checklist, Section 4 (Directory.Build.props) and Section 5 (CPM)
