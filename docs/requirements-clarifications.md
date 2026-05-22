# Business Requirements — Clarifications & Assumptions

Status: **Pending client confirmation** · Date: 2026-05-21

This document scrutinises the business rules in the assessment brief
([`requirements/NET Senior Developer Tech Assessment.md`](./requirements/NET%20Senior%20Developer%20Tech%20Assessment.md))
and surfaces the points it leaves ambiguous. The brief defines the happy-path flow well but
is silent on several rules a reviewer will inevitably test (edits, cancellations after partial
approval, who sees what, validation limits).

> **This is a client-facing artifact.** The development team does **not** own these business
> decisions — the client does. Each item below pairs the **ambiguity** with the team's
> **recommended assumption** (our best-judgement reading of the brief). We will build to the
> recommended assumption so work can proceed, but every item remains **open until the client
> confirms or corrects it**. Items marked **⚠ HIGH IMPACT** materially change the data model
> or permission logic and most need an explicit client decision.

---

## A. Workflow & Lifecycle

### A1 — Can a request be edited after submission? ⚠ HIGH IMPACT
The brief lists create/save-draft/submit but never says whether fields can change once
submitted.
**Recommended assumption (pending client confirmation):** A request is editable **only while in `Draft`**. Once submitted it is
read-only except for workflow actions (approve/reject/cancel) and remarks.

### A2 — How far can a requestor cancel? "Cancel if not yet approved" ⚠ HIGH IMPACT
"Not yet approved" is ambiguous: does it include `PendingFinanceReview` (i.e. after the
manager already approved)?
**Recommended assumption (pending client confirmation):** Requestor may cancel only in
**`Draft` and `PendingManagerApproval`** — i.e. up to but not including the point a manager
approves. Once a manager has approved (`PendingFinanceReview`), the request is committed to
the finance stage and can no longer be cancelled by the requestor.
**Alternative reading to confirm:** a looser interpretation of "not yet approved" would also
permit cancellation during `PendingFinanceReview` (any state before final `Approved`). We
recommend the tighter rule above; the client should confirm which they intend.

### A3 — Is rejection terminal, or can a request be revised and resubmitted?
The brief shows `Rejected` as an end state with no rework arrow.
**Recommended assumption (pending client confirmation):** `Rejected` is **terminal**. To proceed, the requestor creates a new
request. (No send-back/rework loop, no resubmit.)

### A4 — When Finance rejects, does it go to the requestor or back to the manager?
The flow diagram sends both manager-reject and finance-reject straight to `Rejected`.
**Recommended assumption (pending client confirmation):** Finance rejection goes directly to **`Rejected`** (terminal); it is not
returned to the manager for another pass.

### A5 — Are approval/rejection remarks mandatory?
The brief says reviewers "can add remarks" but does not say when they are required.
**Recommended assumption (pending client confirmation):** Remarks are **required on Reject** (a reason must be given) and
**optional on Approve**.

### A6 — Does the requested amount change the path (e.g. small amounts skip Finance)?
The brief shows a single fixed linear flow with no thresholds.
**Recommended assumption (pending client confirmation):** **No amount-based routing.** Every submitted request goes
Manager → Finance regardless of amount.

---

## B. Roles, Routing & Permissions

### B1 — Does a request route to a specific manager, or do all managers share one queue? ⚠ HIGH IMPACT
The brief says "Manager can view pending manager approvals" but never defines manager↔requestor
assignment.
**Recommended assumption (pending client confirmation):** **Shared queue** — any user with the `Manager` role sees all requests in
`PendingManagerApproval`; any `FinanceAdmin` sees all in `PendingFinanceReview`. No per-user
routing or org hierarchy.

### B2 — Is approval scoped by department? ⚠ HIGH IMPACT
`Department` is a form field, but the brief never says approval is department-restricted.
**Recommended assumption (pending client confirmation):** **Not department-scoped.** Department is informational on the request;
it does not restrict who can approve.

### B3 — Can the System Admin approve or reject requests?
The brief grants SystemAdmin "view all requests, view workflow history, manage sponsorship
types" — approval is **not** listed.
**Recommended assumption (pending client confirmation):** SystemAdmin is **not part of the approval chain** — read-all + audit +
manage sponsorship types only.

### B4 — Can a user who is both Manager and Requestor approve their own request?
Not addressed.
**Recommended assumption (pending client confirmation):** **No self-approval.** A user cannot perform a review action on a request
they created, even if they hold the reviewing role.

### B5 — Who can see Drafts?
"System Admin can view all requests" — does "all" include other users' unsubmitted drafts?
**Recommended assumption (pending client confirmation):** **Drafts are private to their owner.** SystemAdmin's "view all" covers
submitted requests (`PendingManagerApproval` onward); drafts are excluded until submitted.

### B6 — How are users created — self-registration or admin-provisioned?
The brief lists logins for four roles but no signup flow.
**Recommended assumption (pending client confirmation):** **No public self-registration.** Accounts are seeded/provisioned; each
user holds **exactly one role**.

---

## C. Form Fields, Data & Validation

### C1 — "Requestor Name": free text, or the logged-in user? ⚠ HIGH IMPACT
The form lists "Requestor Name" as a field, yet the submitter is already authenticated — can
someone submit on behalf of another person?
**Recommended assumption (pending client confirmation):** **Auto-filled from the logged-in account and read-only.** No
"submit on behalf of" capability. (`Department` likewise defaults from the user's profile but
is editable on the request.)

### C2 — Is "Sponsorship Type" a managed lookup, and is it required?
It appears under Basic Information and SystemAdmin "manages sponsorship types".
**Recommended assumption (pending client confirmation):** **Required**, selected from the SystemAdmin-managed lookup list
(seeded with a few common types).

### C3 — What currency is "Requested Amount", and what are its limits?
Currency and bounds are unspecified.
**Recommended assumption (pending client confirmation):** **Single currency** (display label only, e.g. USD), stored as `decimal`;
must be **> 0** with a sane upper bound. No multi-currency / FX.

### C4 — Must "Event Date" be in the future?
Unspecified.
**Recommended assumption (pending client confirmation):** Event Date **must be today or later** at submission time.

### C5 — Supporting Document: one file or many? Type/size limits?
"Document upload can be optional."
**Recommended assumption (pending client confirmation):** **Multiple** attachments allowed; common types (pdf/doc/docx/images);
per-file size cap (e.g. 10 MB). Upload remains optional.

### C6 — Are reviewers allowed to edit request field values?
Not addressed.
**Recommended assumption (pending client confirmation):** **No.** Managers/Finance can only approve/reject and add remarks; they
cannot alter the requestor's field values.

---

## D. Audit & History

### D1 — What does "audit history" capture?
The brief requires "audit history" and "workflow history" without defining granularity.
**Recommended assumption (pending client confirmation):** An immutable trail of every **status transition** (actor, from-status,
to-status, remarks, timestamp). Field-level change tracking is out of scope. This is the **workflow history** feature (`WorkflowHistory`), separate from admin audit (D2).

### D2 — What does the SystemAdmin audit trail capture?
**Recommended assumption (pending client confirmation):** A separate, SystemAdmin-only **admin audit** store (`audit_events`) for mutating operations outside workflow transitions: request create/update, attachment upload, sponsorship-type CRUD, user creation, and auth events (login, logout, profile/password change). **Strictly isolated** from D1 — no shared table, FK, or merged timeline. Workflow transitions (submit/cancel/approve/reject) are **not** duplicated in admin audit.

---

## E. Confirmed Out of Scope (from the brief's "not a production system" note)

Email/real-time notifications · multi-tenancy / org hierarchy · amount-based approval tiers ·
rework/send-back loops · multi-currency · public self-registration · field-level audit diffing.

---

## How to use this document

This is the list of open business questions to walk through with the **client**. For each
item, the team's **recommended assumption** is what we will build to in the meantime so
implementation is not blocked. The client should review every item and confirm or correct it —
especially the **⚠ HIGH IMPACT** items (A1, A2, B1, B2, C1), which shape the data model and
permission logic. Any client correction is recorded here (and the build adjusted) before sign-off.
