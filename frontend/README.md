# Frontend — Sponsorship Request Approval Flow

React 19 + TypeScript + Vite frontend for the Sponsorship Request Approval Workflow.

## Prerequisites

- Node.js 22+
- npm 10+

## Development

```bash
npm install       # install dependencies
npm run dev       # start dev server at http://localhost:5173
```

## Build

```bash
npm run build     # type-check + production bundle → dist/
npm run preview   # serve the production build locally
```

## Lint & Format

```bash
npm run typecheck      # tsc --noEmit (strict mode)
npm run lint           # eslint .
npm run lint:fix       # eslint . --fix
npm run format:check   # prettier --check .
npm run format         # prettier --write .
```

## Tech Stack

| Concern    | Library                                                |
| ---------- | ------------------------------------------------------ |
| Build      | Vite 8                                                 |
| UI         | React 19 + TypeScript (strict)                         |
| Linting    | typescript-eslint (strict) + eslint-plugin-react-hooks |
| Formatting | Prettier                                               |

## Directory Structure (per HLD §9)

```
src/
├─ app/         # Router, providers, layout
├─ features/
│  ├─ auth/     # Login, token/refresh, role context
│  ├─ requests/ # Create/edit form, my-requests, detail + history
│  ├─ approvals/# Manager queue, finance queue, actions
│  └─ admin/    # All-requests view, sponsorship-type CRUD
├─ lib/         # Typed API client, query hooks, Zod schemas
└─ components/  # shadcn/ui-based shared components
```
