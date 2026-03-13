# UML Guidelines — Clinic Management API

This directory contains all **PlantUML** diagrams for the project.

---

## Basics

Every diagram must start with:

`puml
@startuml
!define USE_CUSTOM_THEME
!include ../config/style.puml
`

And end with:

`puml
@enduml
`

---

## Tools Setup

- Create the tools folder

`New-Item -ItemType Directory -Path "docs\uml\tools" -Force`

- Download plantuml.jar (v1.2026.2 - GPL version)

`Invoke-WebRequest -Uri "https://github.com/plantuml/plantuml/releases/download/v1.2026.2/plantuml-1.2026.2.jar" -OutFile "docs\uml\tools\plantuml.jar"`

- Verify the file downloaded

`Get-Item "docs\uml\tools\plantuml.jar" | Select-Object Name, Length`

- Quick test (make sure Java is installed)

`java -jar "docs\uml\tools\plantuml.jar" -version`

- Generate image (saves into an 'exports' subfolder)

`java -jar "docs\uml\tools\plantuml.jar" -tsvg -o "exports" "docs\uml\sprint2\sequence.puml"`

- Generate all images (saving them to respective 'exports' folders)

`Get-ChildItem -Path "docs\uml" -Filter *.puml -Recurse | Where-Object { $_.Name -ne "style.puml" } | ForEach-Object { $jar = "docs\uml\tools\plantuml.jar"; $file = $_.FullName; java -jar $jar -tsvg -o exports $file }`

- Generate only sprint 2 images

`Get-ChildItem -Path "docs\uml\sprint2" -Filter *.puml | ForEach-Object { java -jar "docs\uml\tools\plantuml.jar" -tsvg -o exports $_.FullName }`

---

## Folder Structure

```text
docs/uml/
+-- config/
|   +-- style.puml          <-- Unified dark theme
+-- Diagrams-Readme.md
+-- sprint1/
|   +-- exports/            <-- Generated SVG outputs (.svg)
|   +-- component.puml      <-- Architecture diagram
|   +-- sequence.puml       <-- Request flow
+-- sprint2/
|   +-- exports/            <-- Generated SVG outputs (.svg)
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

- Dark theme (default): Add `!define USE_CUSTOM_THEME` before the include
- Light theme (fallback): Remove the define line

---

## Notes

- All styling is centralized in `config/style.puml` — do not add skinparam in diagrams.
- Keep diagrams focused — one concern per file.
- Use `sprint{N}/` folders to match the project Roadmap.
