# SponTrack — UI Design System

**Status:** Signed off via static prototype · **Prototype:** `docs/ui-mockups/` · **Task:** T3.0

This document is the implementer reference for T3.1–T3.4. The clickable HTML mockups are the visual
source of truth; this file captures tokens, IA, components, and patterns in prose.

---

## 1. Product & brand

| Item | Value |
|------|-------|
| Product name | **SponTrack** |
| Tagline (login panel) | Internal sponsorship request & approval workflow |
| Primary font | [DM Sans](https://fonts.google.com/specimen/DM+Sans) |
| Monospace (amounts, IDs) | [DM Mono](https://fonts.google.com/specimen/DM+Mono) |

---

## 2. Design tokens

Mapped from `docs/ui-mockups/app-shell.css` → Tailwind / shadcn theme variables in T3.1.

### 2.1 Color

| Token | CSS variable | Hex | Usage |
|-------|--------------|-----|-------|
| Brand | `--brand` | `#4A3FC8` | Primary actions, logo accent, focus rings |
| Brand dark | `--brand-dark` | `#332C9A` | Pending-manager badge text |
| Brand light | `--brand-light` | `#EEEDFE` | Metric icons, hover surfaces |
| Brand mid | `--brand-mid` | `#7A72DC` | Focus borders |
| Surface | `--surface` | `#FFFFFF` | Cards, topbar, sidebar |
| Background | `--bg` | `#F4F3FB` | Page background |
| Border | `--border` | `#E4E2F4` | Default borders |
| Text primary | `--text-primary` | `#1A1830` | Headings, body |
| Text secondary | `--text-secondary` | `#6B6894` | Subtitles, labels |
| Text hint | `--text-hint` | `#AAA8C8` | Placeholders, meta |
| Success | `--success` / `--success-bg` | `#0F6E56` / `#E1F5EE` | Approved badge, approve buttons |
| Warning | `--warning` / `--warning-bg` | `#854F0B` / `#FAEEDA` | Pending finance, 409 banners |
| Danger | `--danger` / `--danger-bg` | `#A32D2D` / `#FCEBEB` | Rejected, reject, errors |
| Info | `--info` / `--info-bg` | `#185FA5` / `#E6F1FB` | Info banners (admin draft notice) |
| Gray | `--gray-bg` / `--gray-text` | `#F1EFE8` / `#5F5E5A` | Draft / cancelled badges |

**shadcn mapping (T3.1):** map `--brand` → `--primary`, `--danger` → `--destructive`, `--bg` → `--background`, `--surface` → `--card`, `--border` → `--border`, `--text-primary` → `--foreground`.

### 2.2 Typography

| Element | Size | Weight |
|---------|------|--------|
| Page title | 20px | 600 |
| Page subtitle | 13px | 400 |
| Card / modal title | 16px | 600 |
| Body / table | 13–14px | 400–500 |
| Form label | 12px | 500 |
| Metric value | 28px | 600 |
| Badge | 11px | 500 |

### 2.3 Spacing & layout

| Token | Value |
|-------|-------|
| Border radius (default) | 8px (`--r`) |
| Border radius (large) | 14px (`--r-lg`) |
| Sidebar width | 220px |
| Topbar height | 56px |
| Main content offset | `margin-left: 220px; padding-top: 56px` |
| Metric grid (requestor) | 5 columns |
| Metric grid (other roles) | 4 columns (no Drafts card) |

### 2.4 Responsive notes

Mockups target **desktop-first** (≥1280px). For T3.1+:

- **≤1024px:** collapse sidebar to icon rail or drawer; stack metric cards 2×2.
- **≤640px:** single-column forms; table → card list only; hide grid toggle default to list.

---

## 3. Information architecture

### 3.1 Entry flow

```
login.html → role dashboard (by demo account / JWT role)
```

Demo accounts (prototype): `requestor@`, `manager@`, `finance@`, `admin@spontrack.my` — password `Demo@1234`.

### 3.2 Role navigation

| Role | Sidebar sections | Notes |
|------|------------------|-------|
| **Requestor** | Overview → Dashboard; Requests → My Requests, Drafts, Pending Approval, Approved, Rejected; Account → Profile (stub) | Only role with draft nav + metrics |
| **Manager** | Overview → Dashboard; Account → Profile (stub) | Queue merged into dashboard |
| **Finance Admin** | Overview → Dashboard; Account → Profile (stub) | Same shell as manager |
| **System Admin** | Overview → Dashboard; Administration → Sponsorship Types; Account → Profile (stub) | No separate all-requests route |

Legacy redirect stubs (`approval-queue-*.html`, `admin-all-requests.html`, `approval-review.html`) forward to the canonical screens above.

### 3.3 Draft visibility rule (B5)

**Draft-related UI is requestor-only:**

- Draft metric card, Draft status filter, Draft sidebar link, and draft list rows appear **only** on `dashboard.html`.
- Manager, Finance, and Admin dashboards show **submitted requests only** — no draft metrics, filters, or rows.
- Admin dashboard includes an info banner: *“Submitted requests only — private drafts are visible only to their requestor.”*

---

## 4. Screen inventory

| Screen | Mockup file | Primary roles |
|--------|-------------|---------------|
| Index / screen map | `index.html` | All |
| Login | `login.html` | Public |
| Requestor dashboard | `dashboard.html` | Requestor |
| Manager dashboard | `dashboard-manager.html` | Manager |
| Finance dashboard | `dashboard-finance.html` | Finance Admin |
| Admin dashboard | `dashboard-admin.html` | System Admin |
| Request detail | `request-detail.html?id=&role=` | All (role controls actions) |
| Concurrency conflict | `concurrency-conflict.html` | Manager (409 example) |
| Sponsorship types | `admin-sponsorship-types.html` | System Admin |
| Loading state | `loading-state.html` | Pattern reference |
| Error state | `error-state.html` | Pattern reference |

---

## 5. Per-screen layouts

### 5.1 App shell (authenticated)

Shared across dashboards and detail:

- **Topbar:** brand (links home), optional search (requestor), user chip (avatar initials, name, role).
- **Sidebar:** role nav + Sign out footer.
- **Main:** page header (title + subtitle + primary action) → content.
- **Footer:** copyright + help links (stubs).

### 5.2 Login

Split layout: branded left panel (product story) + right form card (email, password, Sign in). Loading spinner on submit. Error alert on invalid credentials. *Out of scope for MVP:* Remember me, Forgot password (disabled stubs).

### 5.3 Role dashboards

**Common pattern:** metric cards → search + filters + list/grid toggle → data table or card grid → pagination.

| Role | Metrics | Row actions |
|------|---------|-------------|
| Requestor | Total, Drafts, Pending, Approved, Rejected | Edit (draft), Cancel (draft + pending mgr), View |
| Manager | Total, Pending, Approved, Rejected | Approve, Reject, View (when pending mgr) |
| Finance | Total, Pending, Approved, Rejected | Approve, Reject, View (when pending fin) |
| Admin | Total, Pending, Approved, Rejected | View only |

**Create/edit (requestor):** modal dialog — not a separate route.

### 5.4 Request detail

Two-column layout:

- **Left:** request fields card; supporting documents (upload for requestor, read-only for others).
- **Right:** workflow history timeline (action, actor, timestamp, optional remarks).

Header: breadcrumb, title, status badge, back link. Reviewers see Approve/Reject bar when stage matches role.

### 5.5 Admin sponsorship types

Table: Name, Description, Active requests, Actions. Modals: Add/Edit type, Delete confirm, Delete blocked (in use).

---

## 6. Status badge system

Six request statuses — pill badge with dot indicator:

| Status | CSS class | Background | Text |
|--------|-----------|------------|------|
| Draft | `badge-draft` | gray-bg | gray-text |
| Pending Manager Approval | `badge-pending-mgr` | brand-light | brand-dark |
| Pending Finance Review | `badge-pending-fin` | warning-bg | warning |
| Approved | `badge-approved` | success-bg | success |
| Rejected | `badge-rejected` | danger-bg | danger |
| Cancelled | `badge-cancelled` | gray-bg | text-hint |

**shadcn mapping:** custom `Badge` variant enum or `cva` variants (`draft`, `pendingManager`, `pendingFinance`, `approved`, `rejected`, `cancelled`).

---

## 7. UI state patterns (four states + toast)

| State | Pattern | Mockup reference |
|-------|---------|------------------|
| **Loading** | Skeleton metric cards + skeleton table rows + info banner with spinner | `loading-state.html` |
| **Empty** | Centered icon + message inside table card when filter returns zero rows | Embedded in dashboards |
| **Error** | Danger banner + empty-state card + Retry button | `error-state.html` |
| **Success** | Toast notification (bottom-right, auto-dismiss ~3s); green for success, red for errors | All dashboards + detail |

### 7.1 Concurrency (409)

Warning banner at top of detail: *“This request was already actioned…”* — disable Approve/Reject, offer refresh/back. See `concurrency-conflict.html`.

### 7.2 Banners

| Variant | Class | Use |
|---------|-------|-----|
| Info | `banner-info` | Admin draft exclusion notice |
| Warning | `banner-warning` | 409 conflict |
| Danger | `banner-danger` | Load failures |

---

## 8. shadcn/ui component mapping

Implement in `frontend/src/components/` during T3.1–T3.4:

| Mockup pattern | shadcn component(s) |
|----------------|---------------------|
| Buttons (primary, outline, danger, sm) | `Button` + variants |
| Text inputs, selects, textarea | `Input`, `Select`, `Textarea`, `Label` |
| Modal dialogs | `Dialog` |
| Status badges | `Badge` (custom variants) |
| Data tables | `Table` |
| Metric cards | `Card` + composition |
| Sidebar nav | Custom `Sidebar` or `nav` + `Button` ghost |
| User chip | `Avatar` + text |
| Toast | `Sonner` or `Toast` |
| Skeleton loading | `Skeleton` |
| Alert banners | `Alert` |
| Pagination | `Button` group or `Pagination` |
| File upload zone | `Input type="file"` + drop zone styling |
| Search | `Input` with icon |

---

## 9. Form fields — create/edit request

| Field | Required | Notes |
|-------|----------|-------|
| Request title | Yes | |
| Requestor name | — | **Read-only**, populated from `/me` (C1) |
| Sponsorship type | Yes | Select from lookup |
| Department | Yes | Select |
| Event / organisation | Yes | |
| Event date | Yes | Date picker; today or later |
| Requested amount (RM) | Yes | Number, > 0 |
| Purpose / justification | Yes | Textarea |
| Expected benefit | No | Textarea |

Footer actions: **Cancel** (close), **Save draft**, **Submit request**.

---

## 10. Accessibility

- Landmarks: `header`, `nav`, `main`, `footer`; dialogs use `role="dialog"` + `aria-modal`.
- Form labels associated via `for` / `id`.
- Live regions: toast (`aria-live="polite"`), error alerts (`role="alert"`).
- Focus: visible focus rings on inputs (brand-mid border + shadow); trap focus in modals (T3.1+).
- Color: status badges pair background + text; do not rely on color alone — badge includes text label + dot.
- Keyboard: Esc closes modals; table rows actionable via explicit buttons (not row-only click in React).

---

## 11. Follow-ups (deferred)

| Item | Target task |
|------|-------------|
| Profile page | Backlog |
| Notifications / Settings topbar icons | Backlog (requestor mockup shows stubs) |
| Remember me / Forgot password | Out of scope (B6) |
| Mobile sidebar drawer | T3.1 responsive pass |

---

## 12. Sign-off checklist (T3.0)

- [x] `docs/ui-design.md` matches prototype
- [x] `docs/ui-mockups/index.html` navigates all key screens
- [x] Draft UI requestor-only
- [x] Admin uses dashboard only (no all-requests page)
- [x] Badges + four states documented

**Reviewed:** static HTML prototype in repo (T3.0, main branch).
