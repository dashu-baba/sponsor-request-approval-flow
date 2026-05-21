# High-Level Design & Architecture

Project: **Sponsorship Request Approval Workflow**
Status: **Proposed** · Date: 2026-05-21

Companion to [`requirements-clarifications.md`](./requirements-clarifications.md). This is the
high-level architecture; a detailed implementation plan follows separately.

---

## 1. Goals

Build a workflow-driven sponsorship-approval module that demonstrates: clean backend
architecture, an explicit approval state machine, role-based access control, integrated
frontend/backend, clean API design, and reproducible deployment to live URLs.

---

## 2. Technology Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Runtime | **.NET 10** | Latest LTS-track SDK (`10.0.x`). |
| API | ASP.NET Core Web API | Thin endpoints, problem-details errors. |
| Data | **EF Core 10 + Npgsql**, **PostgreSQL 17** | `timestamptz`, `numeric` money. |
| Identity/Auth | ASP.NET Core Identity + **JWT** | Access token + httpOnly refresh cookie. |
| Validation | **FluentValidation** (API) / **Zod** (UI) | Mirrored rules. |
| CQRS / mediator | **MediatR v13+** | Commands/queries + pipeline behaviors. Free Community tier (see note). |
| Mapping | **AutoMapper** | DTO mapping. Free Community tier (see note). |
| Logging | **Serilog** | Structured logs. |
| API docs | **Microsoft.AspNetCore.OpenApi + Scalar UI** | The mandated docs URL. |
| Storage | **MinIO** (S3-compatible) via AWS SDK | Portable to real S3. |
| Frontend | **React 19 + TypeScript + Vite** | TanStack Query, React Router, RHF + Zod. |
| UI kit | **shadcn/ui** (Tailwind + Radix) | Clean, accessible. |
| Tests | **xUnit + FluentAssertions + Testcontainers** | Unit + integration. |
| Infra | **Docker Compose + nginx + Let's Encrypt** | Single VPS. |

---

## 3. System Context

```
                       ┌──────────────────────────── VPS (Docker Compose) ───────────────────────────┐
   Browser  ──HTTPS──▶ │  nginx  ──/──▶ React SPA (static)                                            │
   (4 roles)           │         ──/api──▶ ASP.NET Core API ──▶ PostgreSQL                            │
                       │                              │       └──▶ MinIO (documents, S3 API)          │
                       │         ──/scalar──▶ OpenAPI / Scalar docs                                   │
                       │  Let's Encrypt TLS                                                            │
                       └──────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Backend Architecture (Clean + CQRS-lite)

```
backend/
├─ Domain/             # Entities, enums, workflow state machine, domain rules. No external deps.
├─ Application/        # Commands/Queries + handlers, DTOs, validators, port interfaces,
│                      #   MediatR pipeline behaviors (validation, logging).
├─ Infrastructure/     # EF Core DbContext + migrations, repositories, Identity, JWT service,
│                      #   MinIO storage adapter, audit-history interceptor.
├─ Api/                # Endpoints (thin), DI wiring, middleware, OpenAPI, Serilog.
└─ tests/
   ├─ Domain.Tests/         # State machine + RBAC rules (fast unit tests).
   ├─ Application.Tests/     # Handler behavior with fakes.
   └─ Api.IntegrationTests/  # Testcontainers Postgres + WebApplicationFactory.
```

**Dependency rule:** `Api → Application → Domain`; `Infrastructure → Application/Domain`.
Domain depends on nothing. Application defines interfaces; Infrastructure implements them.

**CQRS-lite:** each use case is a `Command`/`Query` + `IRequestHandler` dispatched via
**MediatR**; pipeline behaviors run FluentValidation and logging. DTO mapping via **AutoMapper**.

> **Licensing note.** AutoMapper and MediatR moved to commercial dual-licensing
> (RPL-1.5 + commercial) under Lucky Penny Software on 2025-07-02. Both remain **free under
> their Community tier** for non-production/evaluation use and for organizations under
> **$5M USD annual revenue**, which covers this assessment. If the client productionizes
> this in an organization above that threshold, a commercial license would be required, and
> the RPL-1.5 copyleft terms would apply to redistribution.

---

## 5. Domain Model

| Entity | Key fields |
|--------|-----------|
| `User` / `Role` | Identity-managed; roles: Requestor, Manager, FinanceAdmin, SystemAdmin. |
| `SponsorshipRequest` | Title, RequestorName, Department, SponsorshipTypeId, EventName, EventDate, RequestedAmount (`decimal`), Purpose, ExpectedBenefit?, Remarks?, Status, RequestorId, timestamps, **concurrency token** (Postgres `xmin`). |
| `SponsorshipType` | Lookup; managed by SystemAdmin. |
| `WorkflowHistory` | RequestId, ActorId, FromStatus, ToStatus, Remarks, OccurredAt (immutable audit). |
| `Attachment` | RequestId, ObjectKey (MinIO), FileName, ContentType, SizeBytes. |

Amounts use C# `decimal` mapped to Postgres **`numeric(18,2)`** — **never** the Postgres `money`
type (locale-dependent via `lc_monetary`, fixed scale). All timestamps `timestamptz` (UTC). Schema uses
**snake_case** identifiers via `EFCore.NamingConventions`. `SponsorshipRequest` carries an
**optimistic-concurrency token** (`xmin`) so simultaneous approve/reject attempts can't both win.

---

## 6. Workflow State Machine

States: `Draft`, `PendingManagerApproval`, `PendingFinanceReview`, `Approved`, `Rejected`, `Cancelled`.

```
Draft ──submit(Requestor)──▶ PendingManagerApproval ──approve(Manager)──▶ PendingFinanceReview ──approve(FinanceAdmin)──▶ Approved
  │                                  │                                            │
  └──cancel(Requestor)──▶ Cancelled  ├──reject(Manager)──▶ Rejected               └──reject(FinanceAdmin)──▶ Rejected
                                     └──cancel(Requestor)──▶ Cancelled
```

| From | Action | Allowed role | To |
|------|--------|--------------|----|
| Draft | Submit | Requestor (owner) | PendingManagerApproval |
| Draft / PendingManagerApproval | Cancel | Requestor (owner) | Cancelled |
| PendingManagerApproval | Approve | Manager | PendingFinanceReview |
| PendingManagerApproval | Reject | Manager | Rejected |
| PendingFinanceReview | Approve | FinanceAdmin | Approved |
| PendingFinanceReview | Reject | FinanceAdmin | Rejected |

The domain rejects any transition not in this table (invalid state → HTTP 409; wrong role →
HTTP 403). Concurrent transitions on the same request are guarded by the optimistic-concurrency
token — the losing writer gets **HTTP 409** rather than a double-transition. Every successful
transition appends a `WorkflowHistory` row.

---

## 7. RBAC

Two enforcement layers:
1. **Endpoint policies** — role claims gate each route (e.g., approve/reject manager step =
   `Manager` only).
2. **Resource/ownership checks in handlers** — a Requestor may only act on their own requests;
   queue queries are scoped (Manager sees `PendingManagerApproval`, Finance sees
   `PendingFinanceReview`). SystemAdmin sees **all submitted requests** (drafts excluded — they
   stay private to their owner per [B5](./requirements-clarifications.md)), plus full history +
   sponsorship-type management; SystemAdmin is **not** part of the approval chain (B3).
3. **No self-approval** — a user who also holds a reviewing role cannot approve/reject a request
   they created (B4). Each user holds exactly one role and accounts are provisioned, not
   self-registered (B6).

---

## 8. API Surface (representative)

| Method | Route | Role |
|--------|-------|------|
| POST | `/auth/login`, `/auth/refresh`, `/auth/logout` | public / authed |
| GET | `/me` | authed |
| GET/POST/PUT | `/requests` | Requestor (own); list scoped by role |
| POST | `/requests/{id}/submit` · `/cancel` | Requestor (owner) |
| POST | `/requests/{id}/approve` · `/reject` | Manager / FinanceAdmin (by stage) |
| GET | `/requests/{id}/history` | owner / reviewer / SystemAdmin |
| POST | `/requests/{id}/attachments` | Requestor (owner) |
| GET/POST/PUT/DELETE | `/sponsorship-types` | SystemAdmin |

Consistent `application/problem+json` errors; pagination + filtering on list endpoints.

---

## 9. Frontend Structure

```
frontend/src/
├─ app/            # Router, providers (QueryClient, auth), layout.
├─ features/
│  ├─ auth/        # Login, token/refresh handling, role context.
│  ├─ requests/    # Create/edit form, my-requests, detail + history, attachments.
│  ├─ approvals/   # Manager queue, finance queue, approve/reject actions.
│  └─ admin/       # All-requests view, sponsorship-type CRUD.
├─ lib/            # Typed API client, query hooks, Zod schemas.
└─ components/     # shadcn/ui-based shared components.
```

Role-aware routing guards; views tailored per role; forms validated with Zod mirroring
backend FluentValidation rules.

---

## 10. Deployment (Single VPS)

`docker-compose.yml` services: `api`, `db` (Postgres), `minio`, `nginx`, and a one-shot
**`migrator`**.
- nginx serves the built SPA and reverse-proxies `/api` + `/scalar`; terminates TLS
  (Let's Encrypt).
- Multi-stage Dockerfiles for API and SPA build.
- **Migrations run via the dedicated `migrator` service** (`dotnet ef database update`) that
  completes before `api` starts (compose `depends_on: condition: service_completed_successfully`);
  the API does **not** migrate on startup. Seeding runs once after migration.
- `.env.example` for secrets/config.
- `docs/deploy.md` runbook: provision VPS, point domain, issue cert, `docker compose up`.

---

## 11. Seed Data

Roles, **4 test accounts (one per role)** with documented credentials, sponsorship types,
and **sample requests covering every status** so reviewers test approvals immediately.

---

## 12. Testing Strategy

- **Unit:** workflow state-machine transitions (valid + invalid), RBAC rules, validators.
- **Integration:** end-to-end API flows against real Postgres (Testcontainers) +
  `WebApplicationFactory`, including a full Draft → Approved happy path and rejection/cancel paths.

---

## 13. Deliverables Mapping

| Brief requirement | Where delivered |
|-------------------|-----------------|
| Git repo + commits + README | Monorepo, incremental commits, `README.md`. |
| Working app (API + UI + DB) | Docker Compose stack. |
| Setup guide (run BE/FE, DB, test logins) | `README.md`. |
| Architecture explanation | This doc (`high-level-design.md`) + `requirements-clarifications.md`. |
| Live URLs (FE, API, docs, logins, repo, notes) | `docs/deploy.md` + README links section. |

---

## 14. Risks / Assumptions

- **Time vs. polish:** brief estimates ~6h; full scope here is larger — sequenced so a
  working vertical slice exists early.
- **Live URL depends on user-provisioned VPS + domain;** candidate supplies all scripts.
- **MinIO/S3 portability** assumed sufficient; no real S3 account required for the assessment.
- Single-node deployment; no HA/autoscaling (out of scope).

---

## 15. Business Rules & Confirmed Assumptions

These are the recommended assumptions from
[`requirements-clarifications.md`](./requirements-clarifications.md) (pending client
confirmation) and where each is enforced in this design. This section is the authoritative
mapping; if the client revises a clarification item, update it here too.

| ID | Rule | Enforced where |
|----|------|----------------|
| A1 | Request is **editable only while `Draft`**; once submitted, fields are read-only (only workflow actions + remarks change). | `PUT /requests/{id}` rejects edits unless status = `Draft`. |
| A2 | Requestor may **cancel only in `Draft` / `PendingManagerApproval`** (not after a manager approves). | §6 state machine. |
| A3 | `Rejected` is **terminal** (no resubmit/rework). | §6 state machine. |
| A4 | Finance rejection goes **directly to `Rejected`** (not back to manager). | §6 state machine. |
| A5 | Remarks **required on Reject**, optional on Approve. | Validator on reject command. |
| A6 | **No amount-based routing** — every request goes Manager → Finance. | §6 (single linear flow). |
| B1 | **Shared queues** — any Manager/FinanceAdmin sees their stage's queue; no per-user routing. | §7 RBAC. |
| B2 | **Not department-scoped** — Department is informational only. | §7 RBAC (no dept filter). |
| B3 | SystemAdmin is **not in the approval chain** (read-all + audit + types). | §6 table + §7. |
| B4 | **No self-approval** — a reviewer cannot action a request they created. | Handler ownership check. |
| B5 | **Drafts are private to their owner**; SystemAdmin "view all" = submitted requests only. | §7 RBAC (query scope). |
| B6 | **No self-registration**; each user holds **exactly one role** (provisioned/seeded). | No `/register` route; seed data. |
| C1 | **Requestor Name auto-filled from the logged-in account, read-only**; Department defaults from profile but is editable. | Set server-side from the authenticated user. |
| C2 | **Sponsorship Type required**, from the SystemAdmin-managed lookup. | Validator + FK. |
| C3 | **Single currency** (display label, e.g. USD); amount stored as `decimal`, must be **> 0** (with a sane upper bound). | `decimal` column + validator. |
| C4 | **Event Date must be today or later** at submission. | Validator on submit. |
| C5 | **Multiple attachments** allowed; common types (pdf/doc/docx/images); per-file cap (e.g. 10 MB); upload optional. | Upload endpoint validation + MinIO. |
| C6 | **Reviewers cannot edit request field values** — only approve/reject + remarks. | No reviewer edit path; §7 RBAC. |
| D1 | **Audit = immutable status-transition trail** (actor, from, to, remarks, timestamp); no field-level diffing. | `WorkflowHistory` (§5) appended on every transition (§6). |

**Out of scope** (confirmed): email/real-time notifications, multi-tenancy / org hierarchy,
amount-based approval tiers, rework/send-back loops, multi-currency, public self-registration,
field-level audit diffing.
