# Task Backlog — Index

Status: **Proposed** · Date: 2026-05-21

The full breakdown of the build into branch-per-task units, per
[`../workflow.md`](../workflow.md). Each task becomes a card file in this folder
(`T<id>-<slug>.md`), authored **just-in-time** before its branch starts so it reflects the
contracts produced by already-merged tasks.

Legend: ⬜ not started · 🟦 in progress · 👀 in review · ✅ merged

---

## Dependency Overview

```
Epic 0 Foundation ─┬─▶ Epic 1 Data & Auth ─┬─▶ Epic 2 Workflow backend ─┬─▶ Epic 3 Frontend ─┬─▶ Epic 4 Delivery
 (sequential)      │   (mostly sequential)  │   (2.1 first, then ∥)      │  (3.1 first, then ∥)│
                   │                         │                            │                     │
 must merge first  └─ everything branches    └─ 2.2 needs 2.1;            └─ each role view     └─ docs/deploy
                      off the merged base       2.3/2.4/2.5 ∥ after 2.1      needs its endpoints    last
```

---

## Tasks

### Epic 0 — Foundation (sequential; merges to `main` before any feature work)
| ID | Title | Depends on | Branch | Status |
|----|-------|-----------|--------|--------|
| T0.1 | Repo + .NET solution scaffold (Clean Arch projects, `.editorconfig`, `Directory.Build.props`, analyzers, `.gitignore`, README skeleton) | — | `chore/T0.1-solution-scaffold` | ✅ |
| T0.2 | Frontend scaffold (Vite + React + TS strict, ESLint/Prettier) + Husky + lint-staged pre-commit hooks + CI workflow + PR template | T0.1 | `chore/T0.2-frontend-and-ci` | ⬜ |
| T0.3 | Docker-Compose walking skeleton (api health endpoint + Postgres + MinIO + nginx + React shell + one-shot `migrator` service, all `up`) + deploy doc skeleton | T0.2 | `chore/T0.3-compose-skeleton` | ⬜ |

### Epic 1 — Data & Auth (mostly sequential)
| ID | Title | Depends on | Branch | Status |
|----|-------|-----------|--------|--------|
| T1.1 | Domain model + EF Core `DbContext` + initial migration (entities, enums, money/`timestamptz`, **snake_case naming**, **`xmin` concurrency token** on `SponsorshipRequest`) | T0.3 | `feat/T1.1-domain-efcore` | ✅ |
| T1.2 | Identity + JWT auth (login/refresh/logout, role claims, httpOnly refresh cookie, `[Authorize]` policies) | T1.1 | `feat/T1.2-identity-jwt` | ⬜ |
| T1.3 | Seed data (4 role accounts, sponsorship types, sample requests across all statuses) | T1.2 | `feat/T1.3-seed-data` | ✅ |

### Epic 2 — Workflow backend (T2.1 first; 2.3/2.4/2.5 parallel after)
| ID | Title | Depends on | Branch | Status |
|----|-------|-----------|--------|--------|
| T2.1 | Request CRUD + save-draft + edit-only-in-Draft + validators + DTO mapping + ownership RBAC (A1, C1, C3, C4) | T1.3 | `feat/T2.1-request-crud` | ✅ |
| T2.2 | Workflow state machine + transitions (submit/cancel/approve/reject) + `WorkflowHistory` audit + no-self-approval + remarks-on-reject (A2–A6, B4, D1) | T2.1 | `feat/T2.2-workflow-engine` | ✅ |
| T2.3 | Sponsorship-type admin CRUD (C2, B3) | T2.1 | `feat/T2.3-sponsorship-types` | ✅ |
| T2.4 | Attachments → MinIO (multiple, type/size limits, optional) (C5) | T2.1 | `feat/T2.4-attachments` | ⬜ |
| T2.5 | Workflow-history endpoint + role-scoped queues/lists (B1, B2, B5) | T2.2 | `feat/T2.5-history-and-queues` | ✅ |

### Epic 3 — Frontend (design first, then shell, then role views)
| ID | Title | Depends on | Branch | Status |
|----|-------|-----------|--------|--------|
| T3.0 | UI/UX design system + static prototype (`docs/ui-design.md` + `docs/ui-mockups/` HTML screens) | T0.2 | `feat/T3.0-ui-design` | ✅ |
| T3.1 | App shell + routing + auth flow (login, token/refresh, role context, route guards) | T3.0, T1.2 | `feat/T3.1-app-shell-auth` | ✅ |
| T3.2 | Requestor dashboard + modal create/edit, detail + history, attachments (draft UI requestor-only) | T3.1, T2.1, T2.4 | `feat/T3.2-requestor-ui` | ✅ |
| T3.3 | Manager/Finance role dashboards + approve/reject (no drafts; 409/403 handling) | T3.1, T2.2, T2.5 | `feat/T3.3-approvals-ui` | ✅ |
| T3.4 | SystemAdmin dashboard (submitted requests) + sponsorship-type CRUD (no separate all-requests page) | T3.1, T2.3, T2.5 | `feat/T3.4-admin-ui` | ✅ |

### Epic 4 — Cross-cutting & Delivery
| ID | Title | Depends on | Branch | Status |
|----|-------|-----------|--------|--------|
| T4.1 | OpenAPI + Scalar docs, problem-details polish, pagination/filtering on lists | T2.5 | `feat/T4.1-api-docs-polish` | ⬜ |
| T4.2 | Testing pass to HLD bar (fill unit/integration gaps; CI coverage) | T3.4, T4.1 | `test/T4.2-test-hardening` | ⬜ |
| T4.3 | Deployment finalize (compose/nginx/Let's Encrypt/env templates) + live bring-up runbook | T4.2 | `chore/T4.3-deploy` | ⬜ |
| T4.4 | Docs finalization (README setup guide, architecture explanation, test logins, live-URLs section) | T4.3 | `docs/T4.4-final-docs` | ⬜ |

### Epic 5 — Account self-service
| ID | Title | Depends on | Branch | Status |
|----|-------|-----------|--------|--------|
| T5.1 | Profile (display name + department) + password change (current-password re-auth, security-stamp access-token invalidation, refresh-token revoke, current device stays signed in) | T1.2, T3.1 | `feat/T5.1-account-self-service` | ⬜ |

---

## Task Card Template

```markdown
# T<id> — <title>

**Branch:** `<type>/T<id>-<slug>`  ·  **Depends on:** <ids or —>  ·  **Status:** ⬜

## Context
<one short paragraph: why this task exists>

## Scope
**In:** <bullets>
**Out:** <bullets — explicitly excluded>

## References
- Brief: <section> · Clarifications: <IDs> · HLD: <sections>

## Roadmap (high-level steps, not full code)
1. ...
2. ...

## Acceptance Criteria (Definition of Done)
- [ ] ...
- [ ] (plus the global DoD in workflow.md §6)

## Test Expectations
- Unit: <what>
- Integration: <what>

## Best-Practices Checklist (subset relevant to this task)
- [ ] Branch created off latest `main` with the exact card name before any code (workflow.md §1/§2).
- [ ] Versions per workflow.md §5: Node 24 (front end) / .NET 10 (back end); latest stable packages, pinned.
- [ ] <other items from workflow.md §5>
```
