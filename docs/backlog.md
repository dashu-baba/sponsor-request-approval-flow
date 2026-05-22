# Backlog — Deferred Items

Status: **Active** · Date: 2026-05-21

Tracked follow-ups: deferred **Should-fix** review findings and **Nice-to-have** items, per
[`workflow.md`](./workflow.md) §4. Each entry: what, why deferred, and any pointer to revisit.

Legend: 💡 nice-to-have · 🔶 deferred should-fix

| ID | Tier | Item | Why deferred / rationale | Revisit when |
|----|------|------|--------------------------|--------------|
| B-001 | 💡 | Switch CI lint/format from whole-repo to **changed-files-only** | At current size whole-repo verify runs in seconds and prevents drift; incremental adds base-ref/diff plumbing for no gain yet (YAGNI). | Repo grows large / CI time becomes painful. |
| B-002 | 💡 | Add a comment documenting the `AnalysisLevel=latest` choice in `Directory.Build.props` | T0.1 review (M1): `latest` is stricter than the template default `latest-recommended`; intentional max-strictness, but undocumented. | Touching `Directory.Build.props`. |
| B-003 | 💡 | Consolidate the two `[*.cs]` sections in backend `.editorconfig` | T0.1 review (M3): valid (sections merge) but harder to read. | Next `.editorconfig` edit. |
| B-004 | 💡 | Add a comment clarifying global `IsPackable=false` intent | T0.1 review (M4): harmless but could silently block a future NuGet publish of a `src/` lib. | If we ever publish a library package. |
| B-005 | 💡 | Drop redundant `Status` single-column index on `sponsorship_requests` | T1.1 CodeRabbit: composite `(status, created_at)` already covers status filters; extra index adds write cost. | When query plans show the composite index is sufficient. |
| B-006 | 💡 | Add `## Implementation Tasks` h2 before Task 1 in `docs/superpowers/plans/2026-05-22-domain-efcore.md` | T1.1 CodeRabbit nitpick: heading hierarchy skips h2. | Next edit of that plan doc. |
| B-007 | 💡 | Refresh token reuse detection — revoke all active refresh tokens for user on revoked-token reuse | T1.2 review: common rotation hardening; MVP works without it. | Security hardening pass or auth incident. |
| B-008 | 💡 | Rate limiting on `/auth/login` and `/auth/refresh` | T1.2 review: security §15; not required for assessment MVP. | Before production exposure or abuse observed. |
| B-009 | 💡 | Assert `WorkflowHistory` field content in integration tests (FromStatus/ToStatus/ActorId/Remarks) | T2.2 final-review Nice: row-count assertions pass but audit semantics untested. | T2.5 history endpoint work or next test hardening pass. |
| B-010 | 💡 | Update stale comment on workflow endpoint group in `RequestEndpoints.cs` | T2.2 final-review nit: comment still says "any authenticated user" after role policies added. | Next touch of `RequestEndpoints.cs`. |
| B-011 | 🔶 | Add integration tests for extended `GET /requests/{id}` reviewer/admin visibility | T2.5 final-review Should: manager OK on submitted; unrelated requestor → 403 on submitted (detail path). | T4.2 test hardening or next requests touch. |
| B-012 | 🔶 | Rename `Cross_user_access_should_return_403` in `RequestCrudTests.cs` | T2.5 final-review Should: GET now expects 404 for draft cross-user; PUT still 403 — name is misleading. | Next edit of `RequestCrudTests.cs`. |
| B-013 | 💡 | Left-join users in `GetRequestHistoryQueryHandler` so missing actor rows still appear | T2.5 final-review Nice: inner join can drop audit entries if user record missing. | Security/audit hardening pass. |
| B-014 | 💡 | Paginate `GET /requests/{id}/history` for long audit trails | T2.5 final-review Nice: unpaginated list may grow large. | T4.1 API polish or production prep. |
| B-015 | 💡 | Responsive sidebar collapse/drawer at ≤1024px | T3.1 review Nice: fixed 220px sidebar; `docs/ui-design.md` §2.4 deferred for T3.1 scope. | T3.1 responsive pass or T3.2 UI polish. |
| B-016 | 🔶 | Move admin `format.ts` helpers to shared `src/lib/format/` | T3.4 review Should: likely reused by T3.2/T3.3 request UIs; defer until those pages land. | T3.2/T3.3 implementer. |
| B-017 | 🔶 | Read-only attachments section on admin request detail | T3.4 review Should: audit trail incomplete without attachments; no frontend attachment pattern yet. | T3.2/T3.3 attachment UI or T3.4 follow-up. |
| B-018 | 💡 | Sponsorship type `isActive` re-activate/toggle in admin UI | T3.4 review Nice: backend soft-delete only; UI shows badge but no re-enable. | Product decision + API if needed. |
| B-019 | 💡 | Split loading on admin detail page (detail vs history) | T3.4 review Nice: single loading gate waits for both queries. | T3.4 polish or T3.2 pattern reuse. |
| B-020 | 💡 | Move `getErrorMessage` to `@/lib/api/api-error` | T3.4 review Nice: mixes presentation helpers with error parsing. | Shared API error helper pass. |
| B-021 | 💡 | Backend `activeRequestCount` on sponsorship-type list DTO | T3.4 resolve: UI derives counts client-side from submitted requests (max 100); accurate server count deferred. | Backend/API polish if dataset grows. |
| B-022 | 🔶 | Server-side search/status/type filters on approver dashboard list | T3.3 final-review Should deferred: client-side filter + `setPage(1)` on change; pagination totals still ignore active filters until API supports query params. | T4.1 API polish or approver list grows beyond one page. |
| B-023 | 🔶 | Server-side search/status/type filters on requestor dashboard list | T3.2 final-review Should deferred: same pattern as B-022; UI documents “current page only” until `ListOwnRequestsQuery` gains filter params. | T4.1 API polish or requestor list grows beyond one page. |
| B-024 | 💡 | Requestor dashboard “All statuses” dropdown toggles overview ↔ `status=all` | T3.2 review Nice: empty select alternates URL state; confusing UX. | T3.2 polish or T4.1. |
| B-025 | 💡 | RTL tests for requestor dashboard error/empty query states | T3.2 review Nice: loading/actions covered; no `ErrorState`/`EmptyState` failure paths. | T4.2 test hardening. |
| B-026 | 💡 | Mount API under real `/api` route group in Kestrel (drop strip-proxy + cookie path coupling) | T4.5 review Nice: strip-proxy forces backend to hardcode gateway mount (`/api/auth` cookie Path); same issue will recur for OpenAPI `servers`, redirects, ProblemDetails URIs. | T4.1 API polish or next auth/deploy touch. |
| B-027 | ✅ | ~~Remove or document unused nginx `/openapi/` and `/scalar/` blocks~~ | T4.1 review: nginx `/openapi/` and `/scalar/` proxy blocks are required for the Scalar deliverable; keep them. | Closed — T4.1 PR #22. |
| B-030 | 💡 | Gate or protect OpenAPI/Scalar in production deployments | T4.1 review Nice: intentionally exposed for assessment; real prod should restrict unauthenticated API docs (info disclosure). | Post-assessment hardening or T4.3 deploy. |
| B-028 | 💡 | Cache security-stamp validation (avoid DB read per authenticated request) | T5.1 final-review Nice: `OnTokenValidated` calls `FindByIdAsync` every request; fine at current scale. | Load grows or auth latency becomes visible. |
| B-029 | 💡 | Note one-time re-login after deploy when JWTs lack `security_stamp` claim | T5.1 final-review Nice: pre-change access tokens 401 immediately after deploy; expected, not a regression. | Next `docs/deploy.md` edit or release notes. |
| B-030 | 💡 | Extract shared integration test helpers (`CreateUserAsync`, `AuthenticatedClientAsync`, mutation body) | T4.2 review Nice (G7): duplicated setup across `WorkflowTransitionTests`, `HistoryAndQueuesTests`, `RequestCrudTests`, etc. | Next integration test touch or test infra refactor. |
| B-031 | 💡 | Add jsonb expression index on `audit_events.metadata->>'requestId'` | T5.3 review Nice: optional card item; seq scan fine at MVP scale. | Audit list latency grows or EXPLAIN shows seq scans on `requestId` filter. |
| B-032 | 💡 | Rename `IAuditService.Record` → `RecordAsync` for task-card alignment | T5.3 review Nice: sync void API is correct (deferred to `SaveChanges`); naming only. | Next audit touch or API consistency pass. |
