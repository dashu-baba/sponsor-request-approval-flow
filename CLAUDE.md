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

## The pipeline (read `workflow.md` §2 — this is mandatory)
Work is **human-driven, one agent per phase**. You will be told your **role** for this run —
**Implement**, **Review**, **Resolve**, or **Final-review** — plus the target task/PR, model, and
effort. **Do only your phase, then PAUSE.** Never advance to another phase on your own. Handoff
state lives **on the PR** (description + comments) — read the PR for context. Record
`Role · Model · Effort` on the PR.

- **Implement:** read the task card in `docs/tasks/T<id>-*.md` + referenced sections; implement to
  the `docs/best-practices/` standards; **show evidence** (build warnings=errors clean, `dotnet
  format`/ESLint/Prettier clean, tests passing) before claiming done; open the PR with the template;
  then pause. Do NOT review your own work.
- **Review:** independently verify the PR against the card + standards; post tiered comments on the
  PR in the §4 format (🔴 Must / 🟡 Should / 🟢 Nice, with location, problem, why, example fix);
  then pause. Do NOT fix or merge.
- **Resolve:** address every PR comment per §4 rules (fix Must always; Should unless deferred; Nice
  if simple, else →`docs/backlog.md`); reply on each thread; push; then pause. Do NOT merge.
- **Final-review:** re-validate comments addressed + branch green, then squash-merge, delete branch,
  update the backlog index; then pause.

Useful skills by area: EF Core/Postgres → `dotnet-data:optimizing-ef-core-queries`; file upload →
`dotnet-aspnet:minimal-api-file-upload`; tests → `superpowers:test-driven-development`,
`dotnet-test:test-anti-patterns`, `dotnet-test:assertion-quality`; solution/MSBuild →
`dotnet-msbuild:directory-build-organization`, `dotnet-nuget:convert-to-cpm`.

## Hard rules
- The enforced config is the source of truth for style/format (`.editorconfig`, `.csproj`,
  ESLint/Prettier) — the best-practice docs hold the *why*.
- No secrets in the repo — `.env.example` only.
- `main` is protected: merge via PR after agent review + explicit human approval.
- Conventional Commits; small, focused changes.
