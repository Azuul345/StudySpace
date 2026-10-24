# StudySpace

A personal study platform for certification exams. Launch exam: AWS Certified Solutions Architect – Associate (SAA-C03). Azure and CompTIA Network+ follow.

Runs locally. Single user, no login in v1.

## Run it

Needs the .NET SDK and Node. From the repo folder, run:

    .start.cmd

It imports the content packs into `studyspace.db`, starts the API on 127.0.0.1:5080 and the web app on http://localhost:5173, and opens the browser. Press Ctrl+C to stop. It works from any folder, for example `C:path	oStudySpacestart.cmd`.

## Start here

- Agents and contributors: read `docs/00-HANDOFF.md` first.
- Requirements: `docs/01-SPEC.md`.
- Design: `docs/02-ARCHITECTURE.md`.
- Content packs: `docs/03-CONTENT-PACK-FORMAT.md` and `content/`.

## Layout

- `docs/`: spec, architecture, content format, security, testing, roadmap, handoff.
- `backend/`: C# ASP.NET Core API (added in P0.5).
- `frontend/`: React + Vite + TypeScript (added in P0.6).
- `tools/`: content importer CLI (added in P2).
- `content/`: study content packs, one folder per exam and version.
