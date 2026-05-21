# Backlog — Deferred Items

Status: **Active** · Date: 2026-05-21

Tracked follow-ups: deferred **Should-fix** review findings and **Nice-to-have** items, per
[`workflow.md`](./workflow.md) §4. Each entry: what, why deferred, and any pointer to revisit.

Legend: 💡 nice-to-have · 🔶 deferred should-fix

| ID | Tier | Item | Why deferred / rationale | Revisit when |
|----|------|------|--------------------------|--------------|
| B-001 | 💡 | Switch CI lint/format from whole-repo to **changed-files-only** | At current size whole-repo verify runs in seconds and prevents drift; incremental adds base-ref/diff plumbing for no gain yet (YAGNI). | Repo grows large / CI time becomes painful. |
