# CLAUDE.md

Project guidance for agents. This file is a **pointer**, not a rulebook — follow the linked docs;
do not restate their contents here.

## What this is
Sponsorship Request Approval Workflow — a workflow-driven enterprise module.
**Stack:** .NET 10 (Clean Architecture + CQRS, MediatR, AutoMapper, EF Core 10) · PostgreSQL 17 ·
React 19 + TypeScript (Vite) · MinIO (S3-compatible) · Docker Compose + nginx.

## Read these first (authoritative)
- `docs/requirements/NET Senior Developer Tech Assessment.md` — the brief.
- `docs/requirements-clarifications.md` — confirmed business rules (A1–D1).
- `docs/high-level-design.md` — architecture, domain model, workflow, RBAC, §15 rules map.
- `docs/workflow.md` — how we build (branch-per-task, tiered review, DoD, CI gates).
- `docs/tasks/` — task cards (the roadmap for each branch).
- `docs/best-practices/` — C#, ASP.NET Core, React, Postgres/EF, security, deployment, testing.

## How to work a task
1. Read the task card in `docs/tasks/T<id>-*.md` and the sections it references.
2. Implement to the standards in `docs/best-practices/` (don't reinvent; follow them).
3. Invoke the relevant skill for the area, e.g.:
   - EF Core / Postgres → `dotnet-data:optimizing-ef-core-queries`
   - File upload → `dotnet-aspnet:minimal-api-file-upload`
   - Tests → `superpowers:test-driven-development`, `dotnet-test:test-anti-patterns`, `dotnet-test:assertion-quality`
   - Solution/MSBuild → `dotnet-msbuild:directory-build-organization`, `dotnet-nuget:convert-to-cpm`
4. **Show evidence before claiming done:** build clean (warnings = errors), `dotnet format` /
   ESLint / Prettier clean, tests passing. Use LSP diagnostics where available.
5. Open a PR using the template; satisfy the Definition of Done (`workflow.md` §6).

## Hard rules
- The enforced config is the source of truth for style/format (`.editorconfig`, `.csproj`,
  ESLint/Prettier) — the best-practice docs hold the *why*.
- No secrets in the repo — `.env.example` only.
- `main` is protected: merge via PR after agent review + explicit human approval.
- Conventional Commits; small, focused changes.
