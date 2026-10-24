# StudySpace

A personal study platform for certification exams. The first exam is **AWS Certified Solutions Architect – Associate (SAA-C03)**. It runs on your own machine, for one user, with no login. It is not affiliated with AWS.

## What it does

- Lessons per topic, with diagrams, linked to the official documentation.
- Practice in several question formats: multiple choice, multiple response, scenario sets, true/false, ordering, matching and typed answers.
- A timed 65-question exam (130 minutes, with early submit), a daily drill, drills per domain or topic, and a wrong-answer review.
- Spaced-repetition flashcards (SM-2).
- Progress per topic and domain, with weak and strong areas and a wrong-answer bank.
- A **Needs checking** screen that lists the questions an automated verifier could not fully confirm, so a person can look them up.

## About the content

Study content ships as versioned packs in `content/`. The SAA-C03 pack was written by AI agents from official AWS documentation and checked by separate verifier agents against each cited source. No human has reviewed it line by line. Treat it as a study aid, check the official docs for anything you rely on, and use the Needs checking screen for the items the verifier flagged. Corrections with a source are welcome as issues or pull requests.

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22.12 or newer (CI uses 24), which includes npm

## Run it

### Windows

From the repository folder:

```powershell
.\start.cmd
```

The first run installs the frontend packages. Each run imports the content packs into `studyspace.db` (a pack version that is already imported is skipped), starts the API on `127.0.0.1:5080` and the web app on <http://localhost:5173>, and opens your browser. Press Ctrl+C to stop both. You can start it from any folder by using the full path to `start.cmd`. Add `-NoBrowser` to skip opening the browser.

### macOS and Linux

Use two terminals from the repository folder.

```bash
# terminal 1: import the content, then start the API
dotnet run --project tools/StudySpace.Import -- import content/aws/saa-c03/2026.10.5 --db studyspace.db
ConnectionStrings__Default="Data Source=$PWD/studyspace.db" dotnet run --project backend/src/StudySpace.Api --no-launch-profile
```

```bash
# terminal 2: start the web app
cd frontend
npm ci
npm run dev
```

Then open <http://localhost:5173>. The web app proxies `/api` to the API on port 5080. Use `--no-launch-profile` so the API listens on 5080 instead of the port in its launch profile.

## Tests

```bash
dotnet test backend/StudySpace.slnx
cd frontend && npm test && npm run lint && npm run build
```

CI runs the same checks, plus a dependency vulnerability audit, on every pull request.

## Content packs

A pack is a folder of JSON files under `content/<provider>/<exam>/<version>/`:

- `pack.json`: the exam, its domains and weights
- `topics/*.json`: one lesson, its scenarios and its questions per topic
- `flashcards.json`: the glossary deck
- `verification.json`: the verifier's report (only verified packs show questions in practice)

The importer validates a pack strictly (unknown fields are rejected, text must be plain, sizes are capped):

```bash
dotnet run --project tools/StudySpace.Import -- validate content/aws/saa-c03/2026.10.5
dotnet run --project tools/StudySpace.Import -- import content/aws/saa-c03/2026.10.5 --db studyspace.db
```

A changed pack gets a new version folder. Importing it updates the content in place and keeps your progress. `content/_templates/` holds the prompts used for the author and verifier agents. A verifier can mark a question `pass` with a `caveat` when part of it could not be confirmed. Those questions appear on the Needs checking screen.

Lessons name diagrams by id in an optional `lesson.diagrams` list. The diagrams themselves are React SVG components in `frontend/src/diagrams/`, so packs never carry markup.

## Layout

- `backend/`: ASP.NET Core API (.NET 10) with Domain, Application, Infrastructure and Api projects, using SQLite through EF Core.
- `frontend/`: React, Vite and TypeScript, tested with Vitest.
- `tools/StudySpace.Import/`: the content importer command-line tool.
- `content/`: study content packs, one folder per exam and version.
- `start.cmd`, `start.ps1`: one-command start on Windows.

## Security

The API only listens on a loopback address and refuses to start on anything else. The server grades answers, so correct answers are never sent before a question is answered. Lesson text is rendered as sanitized Markdown, links are limited to https, and the production build ships a strict Content Security Policy. The app has no login, so do not expose it to a network.

## Contributing

Work happens on feature branches and goes into `main` through pull requests. CI must pass first.