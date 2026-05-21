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
