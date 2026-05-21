<!--
Workflow: docs/workflow.md  ·  Standards: docs/best-practices/  ·  Backlog: docs/backlog.md
-->

## Task
<!-- Which task card does this implement? e.g. T1.2 — Identity + JWT auth -->
- Task: `T<id>` —
- Card: `docs/tasks/T<id>-<slug>.md`

## Summary
<!-- What this PR does and why, in a few bullets. -->
-

## References implemented
<!-- Exact sections this satisfies. -->
- Brief: · Clarifications: · HLD: · Best-practices:

## Evidence (required before review)
<!-- Paste/confirm command output — see workflow.md "show evidence before claiming done". -->
- [ ] Build clean (zero warnings)
- [ ] `dotnet format` / ESLint / Prettier clean
- [ ] Unit + integration tests **passing** (paste summary)
- [ ] CI green
- [ ] Manual test (nice-to-have): <result or "n/a">

## Definition of Done (workflow.md §6)
- [ ] Acceptance criteria in the card met
- [ ] No secrets committed; `.env.example` updated if config changed
- [ ] Relevant docs updated in this PR
- [ ] All Must-fix resolved; Should-fix resolved or deferred→backlog; Nice-to-have logged→backlog

---

## Reviewer findings (tiered — reviewer fills in)
> Classify every finding into exactly one tier (workflow.md §4).

### 🔴 Must-fix (blocker — implementer resolves before merge)
-

### 🟡 Should-fix (human-in-loop — human decides: address now or defer→backlog)
-

### 🟢 Nice-to-have (log to docs/backlog.md; not done now)
-
