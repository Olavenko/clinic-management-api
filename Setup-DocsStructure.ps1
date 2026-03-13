# ============================================================
# Setup-DocsStructure.ps1
# Purpose: Create the docs structure for Clinic Management API
# Author : Mohamed Ali
# Date   : 2026-03-13
# Usage  : cd to your clinic-management-api root, then run:
#          .\Setup-DocsStructure.ps1
# ============================================================

# --- Safety check: make sure we're in the right repo ---
if (-not (Test-Path ".\ClinicManagementAPI.Api")) {
    Write-Host "ERROR: Run this script from the clinic-management-api root folder." -ForegroundColor Red
    Write-Host "Example: cd D:\clinic-management-api" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== Creating Docs Structure ===" -ForegroundColor Cyan

# --- 1. UML Structure ---
$umlDirs = @(
    "docs\uml\config",
    "docs\uml\sprint1",
    "docs\uml\sprint2",
    "docs\uml\sprint3",
    "docs\uml\sprint4",
    "docs\uml\sprint5",
    "docs\uml\sprint6"
)

foreach ($dir in $umlDirs) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Write-Host "  [+] $dir" -ForegroundColor Green
}

# --- 2. ADR Structure ---
$adrDirs = @(
    "docs\ADR\sprint1",
    "docs\ADR\sprint2"
)

foreach ($dir in $adrDirs) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Write-Host "  [+] $dir" -ForegroundColor Green
}

# --- 3. DeferredTasks ---
New-Item -ItemType Directory -Path "docs\DeferredTasks" -Force | Out-Null
Write-Host "  [+] docs\DeferredTasks" -ForegroundColor Green

# --- 4. Releases ---
New-Item -ItemType Directory -Path "docs\releases" -Force | Out-Null
Write-Host "  [+] docs\releases" -ForegroundColor Green

# --- 5. KnowledgeBase ---
New-Item -ItemType Directory -Path "docs\KnowledgeBase" -Force | Out-Null
Write-Host "  [+] docs\KnowledgeBase" -ForegroundColor Green

# ============================================================
# CREATE TEMPLATE FILES
# ============================================================

Write-Host "`n=== Creating Templates ===" -ForegroundColor Cyan

# --- ADR Template ---
$adrTemplate = @"
# ADR 000X: <Concise-Title-With-Hyphens>

- **Status**: Draft
- **Date**: YYYY-MM-DD
- **Owners**: Mohamed Ali
- **Related**: Sprint N
- **Tags**: <keywords>

---

## Context
Describe the problem or background that led to this decision.
Why is this ADR necessary? What issues are being addressed?

## Decision
State the decision clearly and list the main points.
- Adopted tools, frameworks, or patterns
- Important configuration or implementation details
- Any constraints or requirements

## Consequences
- Benefits
- Drawbacks or risks
- Trade-offs

## References
- Links to related files, diagrams, or other ADRs
"@
Set-Content -Path "docs\ADR\ADR-Template.md" -Value $adrTemplate -Encoding UTF8
Write-Host "  [+] docs\ADR\ADR-Template.md" -ForegroundColor Green

# --- ADR Index ---
$adrIndex = @"
# ADR Index — Clinic Management API

This directory contains Architecture Decision Records (ADRs) for the project.
Each ADR documents a significant architectural or tooling decision.

## ADR List

### Sprint 1 — Project Setup & Foundation

| ID   | Title                              | Status   | Date       | File                                              |
|------|------------------------------------|----------|------------|----------------------------------------------------|
| 0001 | Result Pattern Adoption            | Accepted | YYYY-MM-DD | [0001](sprint1/0001-result-pattern.md)             |
| 0002 | Layered Architecture (3-Project)   | Accepted | YYYY-MM-DD | [0002](sprint1/0002-layered-architecture.md)       |
| 0003 | Manual Mapping over AutoMapper     | Accepted | YYYY-MM-DD | [0003](sprint1/0003-manual-mapping.md)             |
| 0004 | Central Package Management         | Accepted | YYYY-MM-DD | [0004](sprint1/0004-central-package-management.md) |

### Sprint 2 — Authentication

| ID   | Title                                  | Status   | Date       | File                                            |
|------|----------------------------------------|----------|------------|-------------------------------------------------|
| 0001 | JWT + Refresh Token Strategy           | Accepted | YYYY-MM-DD | [0001](sprint2/0001-jwt-refresh-token.md)       |
| 0002 | ASP.NET Core Identity Adoption         | Accepted | YYYY-MM-DD | [0002](sprint2/0002-aspnet-identity.md)         |
| 0003 | Data Annotations over FluentValidation | Accepted | YYYY-MM-DD | [0003](sprint2/0003-data-annotations.md)        |

---

## Status Legend
- **Accepted** — Implemented and in use.
- **Draft** — Created but not yet reviewed.
- **Deferred** — Postponed to a later sprint.
"@
Set-Content -Path "docs\ADR\Index-ADR.md" -Value $adrIndex -Encoding UTF8
Write-Host "  [+] docs\ADR\Index-ADR.md" -ForegroundColor Green

# --- DeferredTask Template ---
$deferredTemplate = @"
# Deferred Task — <concise-title-with-hyphens>

- **Status**: Deferred | In-Progress | Done
- **Date Created**: YYYY-MM-DD
- **Owner**: Mohamed Ali
- **Sprint**: N
- **Tags**: <keywords>

---

## Context
Describe the task that was originally planned for this Sprint.
Mention the related ADR or checklist item if available.

## Reason for Deferral
Explain why this task was postponed.

## Next Steps
- When or in which Sprint the task is expected to be executed.
- Any prerequisites or dependencies that must be resolved first.

## References
- Links to ADRs or related project files.
"@
Set-Content -Path "docs\DeferredTasks\DeferredTask-Template.md" -Value $deferredTemplate -Encoding UTF8
Write-Host "  [+] docs\DeferredTasks\DeferredTask-Template.md" -ForegroundColor Green

# --- Release Notes Template ---
$releaseTemplate = @"
# Release Notes — Sprint N

## Features Delivered
- **<Feature / Component>**
  - Concise feature description.
  - Key implementation details.

## Testing
- Unit Tests: (list main test targets)
- Integration Tests: (describe integration flows tested)

## Known Issues
- Describe known limitations.

## Summary
Short final statement about what this Sprint achieved.
"@
Set-Content -Path "docs\releases\Release-Template.md" -Value $releaseTemplate -Encoding UTF8
Write-Host "  [+] docs\releases\Release-Template.md" -ForegroundColor Green

# --- PlantUML style.puml (Dark Theme) ---
$stylePuml = @"
' ------------------------------------------------------------
' File: style.puml (Dark Theme — Corporate Palette, .NET Edition)
' ------------------------------------------------------------
' Purpose   : Unified dark theme for Clinic Management API diagrams
' Author    : Mohamed Ali
' Created   : 2026-03-13
'
' Palette (Corporate — WCAG-friendly on dark):
' - Background .............. #0D1117
' - Foreground .............. #E6EDF3
' - Neutral Border .......... #30363D
' - Primary (Flow/Links) .... #58A6FF  (Blue)
' - Accent (Components) ..... #A371F7  (Purple)
' - Accent (Infra/Packages) . #39C5CF  (Cyan)
' - Success (Actors) ........ #3FB950  (Green)
' - Warning (DB/Highlights) . #D29922  (Amber)
' - Danger (Errors/Notes) ... #F85149  (Red)
' ------------------------------------------------------------

' ===== Theme Toggle =====
!ifdef USE_CUSTOM_THEME

skinparam defaultFontName "Segoe UI, Arial, Helvetica"
skinparam defaultFontSize 14
skinparam defaultFontColor #E6EDF3
skinparam backgroundColor  #0D1117

skinparam ArrowColor       #58A6FF
skinparam ArrowThickness   2
skinparam ArrowFontColor   #E6EDF3
skinparam ArrowFontSize    12

skinparam rectangle {
    BackgroundColor #0D1117
    BorderColor     #58A6FF
    FontColor       #E6EDF3
    RoundCorner     8
}
skinparam component {
    BackgroundColor #0D1117
    BorderColor     #A371F7
    FontColor       #E6EDF3
}
skinparam class {
    BackgroundColor #161B22
    BorderColor     #A371F7
    FontColor       #E6EDF3
    AttributeFontColor #E6EDF3
}
skinparam package {
    BackgroundColor #0D1117
    BorderColor     #39C5CF
    FontColor       #E6EDF3
}
skinparam actor {
    Style           awesome
    BackgroundColor #3FB950
    BorderColor     #3FB950
    FontColor       #0D1117
}
skinparam database {
    BackgroundColor #0D1117
    BorderColor     #D29922
    FontColor       #E6EDF3
}
skinparam note {
    BackgroundColor #161B22
    BorderColor     #F85149
    FontColor       #E6EDF3
}
skinparam sequence {
    ArrowColor                  #58A6FF
    LifeLineBorderColor         #8B949E
    LifeLineBackgroundColor     #0D1117
    ParticipantBackgroundColor  #0D1117
    ParticipantBorderColor      #58A6FF
    ParticipantFontColor        #E6EDF3
    GroupBorderColor            #30363D
    GroupBackgroundColor        #0D1117
}
skinparam usecase {
    BackgroundColor #0D1117
    BorderColor     #A371F7
    FontColor       #E6EDF3
}
skinparam entity {
    BackgroundColor #161B22
    BorderColor     #D29922
    FontColor       #E6EDF3
}
skinparam shadowing   false
skinparam roundcorner 8

!else

' ===== Fallback Defaults (Light Theme) =====
skinparam defaultFontName "Segoe UI, Arial, Helvetica"
skinparam defaultFontSize 14
skinparam defaultFontColor #000000
skinparam backgroundColor  #FFFFFF

skinparam rectangle {
    BackgroundColor #FFFFFF
    BorderColor     #000000
    FontColor       #000000
}
skinparam component {
    BackgroundColor #FFFFFF
    BorderColor     #000000
    FontColor       #000000
}
skinparam class {
    BackgroundColor #FFFFFF
    BorderColor     #000000
    FontColor       #000000
}
skinparam package {
    BackgroundColor #FFFFFF
    BorderColor     #000000
    FontColor       #000000
}
skinparam actor {
    Style           awesome
    BackgroundColor #DDDDDD
    BorderColor     #000000
    FontColor       #000000
}
skinparam database {
    BackgroundColor #FFFFFF
    BorderColor     #000000
    FontColor       #000000
}
skinparam note {
    BackgroundColor #FFFFBB
    BorderColor     #000000
    FontColor       #000000
}
skinparam sequence {
    ArrowColor                  #000000
    LifeLineBorderColor         #000000
    LifeLineBackgroundColor     #FFFFFF
    ParticipantBackgroundColor  #FFFFFF
    ParticipantBorderColor      #000000
    ParticipantFontColor        #000000
}
skinparam usecase {
    BackgroundColor #FFFFFF
    BorderColor     #000000
    FontColor       #000000
}
skinparam shadowing   false
skinparam roundcorner 8

!endif
"@
Set-Content -Path "docs\uml\config\style.puml" -Value $stylePuml -Encoding UTF8
Write-Host "  [+] docs\uml\config\style.puml" -ForegroundColor Green

# --- UML Diagrams Readme ---
$umlReadme = @"
# UML Guidelines — Clinic Management API

This directory contains all **PlantUML** diagrams for the project.

---

## Basics

Every diagram must start with:

```puml
@startuml
!define USE_CUSTOM_THEME
!include ../config/style.puml
```

And end with:

```puml
@enduml
```

---

## Folder Structure

```
docs/uml/
+-- config/
|   +-- style.puml          <-- Unified dark theme
+-- Diagrams-Readme.md
+-- sprint1/
|   +-- component.puml      <-- Architecture diagram
|   +-- sequence.puml       <-- Request flow
+-- sprint2/
|   +-- component.puml
|   +-- sequence.puml       <-- Auth flow (JWT + Refresh Token)
|   +-- class.puml          <-- Entity relationships
+-- sprint3/                <-- Patient & Doctor management
+-- sprint4/                <-- Appointments
+-- sprint5/                <-- Business rules
+-- sprint6/                <-- Polish & deployment
```

---

## Diagram Types for This Project

| Type          | When to Use                                        | Example                          |
|---------------|----------------------------------------------------|----------------------------------|
| **Component** | Show project layers and their dependencies         | Api -> Core -> SQL Server        |
| **Sequence**  | Show request flow through layers                   | Client -> Endpoint -> Service    |
| **Class**     | Show entity relationships and properties           | Patient, Doctor, Appointment     |
| **ERD**       | Show database tables and foreign keys              | Tables with PK/FK relationships  |

---

## Theme Toggle

- Dark theme (default): Add ``!define USE_CUSTOM_THEME`` before the include
- Light theme (fallback): Remove the define line

---

## Notes

- All styling is centralized in ``config/style.puml`` — do not add skinparam in diagrams.
- Keep diagrams focused — one concern per file.
- Use ``sprint{N}/`` folders to match the project Roadmap.
"@
Set-Content -Path "docs\uml\Diagrams-Readme.md" -Value $umlReadme -Encoding UTF8
Write-Host "  [+] docs\uml\Diagrams-Readme.md" -ForegroundColor Green

# --- .gitkeep files for empty sprint folders ---
foreach ($i in 1..6) {
    $keepPath = "docs\uml\sprint$i\.gitkeep"
    if (-not (Test-Path "docs\uml\sprint$i\*.puml")) {
        Set-Content -Path $keepPath -Value "" -Encoding UTF8
    }
}
Write-Host "  [+] .gitkeep files for empty sprint folders" -ForegroundColor DarkGray

# ============================================================
# SUMMARY
# ============================================================
Write-Host "`n=== Done! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Structure created:" -ForegroundColor White
Write-Host "  docs/" -ForegroundColor Yellow
Write-Host "  +-- uml/config/style.puml        (Dark Theme)" -ForegroundColor White
Write-Host "  +-- uml/Diagrams-Readme.md        (Guidelines)" -ForegroundColor White
Write-Host "  +-- uml/sprint1..6/               (Diagram folders)" -ForegroundColor White
Write-Host "  +-- ADR/ADR-Template.md            (Decision template)" -ForegroundColor White
Write-Host "  +-- ADR/Index-ADR.md               (Decision index)" -ForegroundColor White
Write-Host "  +-- DeferredTasks/                 (Deferred work tracking)" -ForegroundColor White
Write-Host "  +-- releases/Release-Template.md   (Release notes template)" -ForegroundColor White
Write-Host "  +-- KnowledgeBase/                 (Lessons learned)" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Green
Write-Host "  1. Review the generated files" -ForegroundColor White
Write-Host "  2. Fill in ADR dates for Sprint 1 & 2 decisions" -ForegroundColor White
Write-Host "  3. Start writing your first PlantUML diagram" -ForegroundColor White
Write-Host ""
