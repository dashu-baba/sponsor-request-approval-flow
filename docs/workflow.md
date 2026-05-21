# Development Workflow

Status: **Active** · Date: 2026-05-21

How this project is built: requirements are broken into small tasks; each task is implemented
on its own branch by an implementer agent following the architecture, design, and best-practice
standards below; an independent reviewer agent reviews it in tiered findings; changes are
addressed; a second review runs; then — on explicit human approval — it is merged.

Authoritative inputs every task must respect:
- [`requirements/NET Senior Developer Tech Assessment.md`](./requirements/NET%20Senior%20Developer%20Tech%20Assessment.md) — the brief.
- [`requirements-clarifications.md`](./requirements-clarifications.md) — confirmed business rules (A1–D1).
- [`high-level-design.md`](./high-level-design.md) — architecture, stack, domain model, §15 rules map.
- This document — standards and process.

---

## 1. Branching Model

- **Trunk-based with short-lived feature branches.** `main` is always green and deployable.
- **Foundation lands first.** Epic 0 (scaffold, CI, lint configs, walking skeleton) merges to
  `main` before any feature branch starts, so every later branch inherits the shared base and
  conflicts stay small.
- **One branch per task.** Branch name: `feat/T<id>-<slug>` (e.g. `feat/T1.2-jwt-auth`),
  `fix/...`, or `chore/...`. Branch off the latest `main`.
- **Rebase before merge.** Update the branch onto latest `main` and re-run CI before merging.
- **Squash-merge** each task PR into `main` to keep history readable (one logical change = one
  commit on `main`).

---

## 2. Task Lifecycle — human-driven, one agent per phase

The pipeline is **driven manually by the human**, **one agent invocation per phase**. For each
invocation the human specifies: the **role** (implement / review / resolve / final-review), the
**target** (task id or PR number), the **model**, and the **reasoning effort**. Agents do **not**
auto-advance — **each phase ends with a pause**. Handoff state lives **on the PR** (description +
review comments), not in any agent's memory, so every phase agent reads the PR to get its context.

Every agent records, in its PR comment (or the PR description for Phase A), this line:

```
Role: <Implement|Review|Resolve|Final> · Model: <opus|sonnet|haiku> · Effort: <low|medium|high>
```

(The model is set when the agent is dispatched; effort is conveyed as an instruction in the
agent's prompt — there is no separate effort knob for subagents.)

| Phase | Role | Does | Must NOT do | Ends by |
|-------|------|------|-------------|---------|
| **A** | Implement | Implements exactly one task on its `feat/…`/`chore/…` branch; self-verifies (build/format/test **evidence**); opens the PR; records role/model/effort. | Review its own work; start another task. | Push + open PR, then **pause**. |
| **B** | Review | A **different** agent reviews the PR and posts **tiered comments on the PR** (format in §4). | Fix anything; merge. | Post comments + summary, then **pause**. |
| **C** | Resolve | Reads the PR comments; addresses each per §4 rule; replies on each thread how it was handled; pushes. | Merge. | Push fixes + replies, then **pause**. |
| **D** | Final review | Re-validates (all comments addressed, branch green); **merges** (squash) + deletes branch + updates the backlog index. | — | Merge, then **pause**. |

- The **implementer and reviewer are always different agents** — no self-review, ever.
- The implementer must **show evidence** (build/format/test output) before claiming done — never
  assert "it works" without proof.
- **Manual testing is encouraged but optional** (nice-to-have): if feasible, run the relevant flow
  and note the result in the PR.
- If a review finds nothing blocking, Phase C may be a no-op and the human may trigger Phase D directly.

---

## 3. Task Card (the "roadmap")

Each task is a card in [`docs/tasks/`](./tasks/). Cards are **roadmap-level**: enough context to
keep the implementation on-rails, not line-by-line code. Every card has:

1. **Context / why** — one short paragraph.
2. **Scope** — explicit in-scope and out-of-scope bullets.
3. **References** — exact sections of the brief / clarifications / HLD this implements
   (e.g. "HLD §6, clarifications A2/B4").
4. **Roadmap** — the ordered high-level steps (not full code).
5. **Acceptance criteria (Definition of Done)** — objective, checkable.
6. **Test expectations** — what unit/integration tests must exist.
7. **Best-practices checklist** — the relevant subset of §5 standards.
8. **Branch name.**

Cards for later epics are authored **just-in-time** (right before their branch starts), so they
reflect the actual contracts produced by already-merged tasks rather than going stale.

---

## 4. Code Review — Tiered Findings

The reviewer classifies every finding into exactly one tier:

| Tier | Meaning | Action |
|------|---------|--------|
| **Must-fix** (blocker) | Correctness, security, broken requirement, failing/absent required test, data-loss risk. | Implementer **must** resolve before merge. |
| **Should-fix** (human-in-loop) | Real quality/maintainability/UX concern, but not a blocker. | **Human decides** per item: address now, or consciously defer (→ backlog). |
| **Nice-to-have** | Optional polish / future improvement. | Logged to [`docs/backlog.md`](./backlog.md) with a one-line rationale; **not** done now. |

Every deferred Should-fix and every Nice-to-have lands in `docs/backlog.md` so nothing is lost.

### Review comment format (Phase B)
The reviewer posts findings **on the PR** — inline review comments where a line is implicated, plus
one summary comment listing every finding. Each finding uses this exact format so the resolver can
act on it unambiguously:

> **[🔴 Must | 🟡 Should | 🟢 Nice] &lt;short title&gt;**
> **Location:** `path/to/file.cs:123` (or "area: …")
> **Problem:** what is wrong.
> **Why it matters:** the impact, and which requirement/standard it violates.
> **Suggested fix:**
> ```csharp
> // a concrete example or numbered steps the dev can follow
> ```

The summary comment ends with the `Role · Model · Effort` line. The reviewer then **pauses** — it
does **not** fix anything, and it does **not** merge.

### Resolution rules (Phase C)
The resolver reads every comment, acts on it, and **replies on that comment's thread** with one of:

- `✅ Resolved — <how / commit>` — **Must:** always. **Should:** unless the human or a rule defers it.
  **Nice:** only when the fix is simple.
- `📋 Deferred → backlog <B-id> — <why>` — a **Should** being deferred, or a **Nice** that isn't simple.

Deferred items are added to `docs/backlog.md` in the same push. The resolver pushes its changes and
**pauses** — it does **not** merge.

---

## 5. Best-Practice Standards (every task must follow)

These are enforced by tooling/CI where possible, not left to memory.

**.NET / C# / ASP.NET Core**
- `Nullable` enabled, `TreatWarningsAsErrors=true`, latest C# language version; Roslyn analyzers
  + `.editorconfig` enforced; `dotnet format` clean.
- Clean Architecture dependency rule respected (Domain has no outward deps).
- Async all the way (`async`/`await`, `CancellationToken` on I/O); no `.Result`/`.Wait()`.
- Input validation at the boundary (FluentValidation); domain invariants in the domain.
- Consistent `application/problem+json` errors; no leaking stack traces.
- Structured logging (Serilog); no `Console.WriteLine`; no logging of secrets/PII.

**Security**
- No secrets in the repo — `.env.example` only; real secrets via env/user-secrets.
- Enforce authN + authZ on every endpoint; ownership/role checks in handlers (RBAC, no
  self-approval per B4); JWT access token + httpOnly refresh cookie.
- Hash passwords via ASP.NET Identity; validate/limit uploads (type + size); parameterised
  queries only (EF Core) — no string-built SQL; OWASP Top 10 awareness; CORS locked down.

**React / TypeScript**
- TypeScript `strict`; ESLint + Prettier clean; no `any` without justification.
- Server state via TanStack Query; forms via React Hook Form + Zod; accessible components.
- No secrets/tokens in `localStorage`; tokens handled per the auth design.

**Postgres / EF Core**
- All schema changes via EF migrations (checked in); money as `numeric`/`decimal`; timestamps
  `timestamptz` (UTC); appropriate indexes/FKs; no N+1 (project to DTOs, use `AsNoTracking` for reads).

**Testing**
- TDD where practical; meet the HLD bar — unit tests for domain/workflow/RBAC + validators;
  integration tests (Testcontainers + `WebApplicationFactory`) for API flows.
- Tests are deterministic and independent; no reliance on external network.

**Comments & docs**
- Comment the *why*, not the *what*; XML docs on public contracts where it adds value.
- Update the relevant doc (README / deploy / backlog) **in the same PR** as the change.

**Commits**
- Conventional Commits (`feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`); small, focused;
  imperative mood.

**Local pre-commit hooks (fast feedback — changed/staged files only)**
- Husky + `lint-staged` run on **staged files only**: `eslint --fix` + `prettier --write`
  (frontend) and `dotnet format --include <staged>` (backend). Keeps commits fast; whole-repo
  conformance is guaranteed by CI (§7), not by the hook.

---

## 6. Definition of Done (global, every task)

A task is mergeable only when **all** are true:
- [ ] Acceptance criteria in the card are met.
- [ ] Builds with zero warnings; `dotnet format` / ESLint / Prettier clean.
- [ ] Required unit + integration tests written and **passing**; evidence shown.
- [ ] CI is green on the PR.
- [ ] No secrets committed; `.env.example` updated if config changed.
- [ ] Relevant docs updated in the same PR.
- [ ] All Must-fix resolved; Should-fix resolved or deferred-to-backlog by human; Nice-to-have
      logged to backlog.
- [ ] Second review approved **and** human gave explicit merge approval.

---

## 7. CI Gates (GitHub Actions, on every PR)

CI verifies the **whole repository** (not just changed files). At this repo's size these checks
run in seconds, and whole-repo verification prevents drift (a file silently going non-conformant
when a rule changes but the file isn't touched). The build/analyzers compile the whole solution
regardless, so format/lint are kept whole-repo for consistency.

- **Backend:** restore → build (warnings = errors, whole solution) →
  `dotnet format --verify-no-changes` (whole solution) → `dotnet test` (incl. Testcontainers
  integration tests).
- **Frontend:** install → typecheck → `eslint .` → `prettier --check .` → build →
  (unit tests if present).
- A PR cannot merge unless CI is green.

> **Future optimization (deferred):** switching CI to changed-files-only is the right move once
> the repo is large and CI time hurts. Tracked in [`backlog.md`](./backlog.md); not done now (YAGNI).

---

## 8. Merge Authority

- **The Phase D (Final-review) agent merges** — after re-validating that every review comment was
  addressed and the branch is green (CI green once CI exists). The human's control point is that
  **they manually trigger Phase D**; invoking it authorizes the merge.
- Squash-merge into `main`; delete the branch; update the task-backlog status to ✅.
- `main` is protected. With a single GitHub account there is no second party to satisfy a review
  requirement, so the owner merges via **admin override** (`gh pr merge --squash --admin`); the real
  gate is "comments addressed + CI green + human triggered Phase D."
- Never force-push `main`; never merge with unresolved Must-fix or red CI.
