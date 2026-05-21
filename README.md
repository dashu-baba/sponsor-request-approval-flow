# Sponsorship Request Approval Workflow

A workflow-driven enterprise module for submitting and approving internal sponsorship requests,
built for a Senior Full-Stack .NET technical assessment.

> **Status:** In active development (foundation phase). Live URLs and full setup steps are added as
> the build progresses.

## What it does
Staff submit sponsorship requests that move through an approval workflow — **Draft → Pending
Manager Approval → Pending Finance Review → Approved**, with **Rejected** and **Cancelled** paths —
with role-based access (Requestor, Manager, Finance Admin, System Admin) and an immutable audit
history.

## Tech stack
- **Backend:** .NET 10, ASP.NET Core (Clean Architecture + CQRS, MediatR, AutoMapper, EF Core 10)
- **Database:** PostgreSQL 17 · **Storage:** MinIO (S3-compatible)
- **Frontend:** React 19 + TypeScript (Vite)
- **Infra:** Docker Compose + nginx · CI via GitHub Actions

## Repository layout
```
backend/    .NET solution (Domain / Application / Infrastructure / Api + tests)
frontend/   React + TypeScript app (added in T0.2)
docs/        specs, design, workflow, best-practice rulebooks, task backlog
.github/     PR template + CI workflows
```

## Documentation
- [Requirements brief](docs/requirements/NET%20Senior%20Developer%20Tech%20Assessment.md)
- [Business requirements — clarifications & assumptions](docs/requirements-clarifications.md)
- [High-level design & architecture](docs/high-level-design.md)
- [Development workflow](docs/workflow.md)
- [Task backlog](docs/tasks/README.md) · [Deferred items](docs/backlog.md)
- [Best-practice rulebooks](docs/best-practices/)

## Getting started
- **Backend:** see [`backend/README.md`](backend/README.md) for build/test.
- **Full stack (Docker):** added in task T0.3; deployment runbook in `docs/deploy.md` (T4.3).

## Test accounts & live URLs
Added once authentication seeding (T1.3) and deployment (T4.3) land.
