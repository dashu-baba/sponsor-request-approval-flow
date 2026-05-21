# Deployment & Docker Best Practice Rules

A practical rulebook for containerizing and deploying the app (Docker, Compose, nginx, TLS).

## Images

### 1. Use multi-stage builds
Build/restore/publish in a build stage; copy only the published output into a small runtime
image. Keep build tooling out of the final image.

### 2. Use slim, pinned base images
Pin specific tags (e.g. the .NET 10 runtime, **`node:24`** — the mandated frontend runtime, see
workflow.md §5 — `postgres:17`) — never `latest`. Pin to a specific patch tag (e.g.
`node:24.x.x-slim`), not a floating `node:24`. Prefer slim/alpine where compatible.

### 3. Run as a non-root user
Create and switch to a non-root user in the runtime image. Containers should not run as root.

### 4. Keep images small and layer-friendly
Copy dependency manifests and restore before copying source so dependency layers cache. Use
`.dockerignore` to exclude `bin/`, `obj/`, `node_modules/`, `.git/`, secrets.

### 5. One concern per image
Each service image does one thing (api, migrator, spa/nginx). Don't bake multiple daemons into one.

## Configuration & Secrets

### 6. Configure via environment
Pass config through environment variables, not baked-in files. Provide a checked-in `.env.example`
with placeholders; never commit real `.env`.

### 7. Never bake secrets into images or compose files
Use env files excluded from git, Docker/CI secrets, or a secrets manager. Secrets must not appear
in image layers or `docker history`.

### 8. Separate config per environment
Distinguish local vs production settings (compose overrides, env files). Production turns off
detailed errors and dev-only diagnostics.

## Compose Topology

### 9. Make startup order explicit and correct
Use `depends_on` with healthchecks (`condition: service_healthy`) so the API waits for a ready
database, not just a started one.

### 10. Run migrations as a dedicated one-shot service
A `migrator` service applies migrations and exits; the API waits for
`condition: service_completed_successfully`. The API does not migrate on startup.

### 11. Define healthchecks for every long-running service
Compose/orchestrators need a real readiness signal (DB `pg_isready`, API `/health`).

### 12. Persist stateful data in named volumes
Database and object-storage data live in named volumes, not the container layer, so restarts/redeploys
don't lose data.

### 13. Don't expose internal services publicly
Only the reverse proxy publishes ports. Database, object storage, and the API sit on an internal
network, reached via the proxy.

## Reverse Proxy, TLS & Networking

### 14. Terminate TLS at the proxy
nginx (or similar) terminates HTTPS, serves the built SPA, and reverse-proxies `/api` and the docs
endpoint. Automate certs (Let's Encrypt) and auto-renew.

### 15. Set proxy headers and limits
Forward `X-Forwarded-*`, configure sensible body-size limits (uploads), timeouts, and gzip.

### 16. Force HTTPS and security headers at the edge
Redirect HTTP→HTTPS, enable HSTS, and apply security headers at the proxy.

## Operations

### 17. Make health/readiness observable
Expose `/health` (liveness) and a readiness check that verifies dependencies (DB, storage).

### 18. Log to stdout/stderr as structured JSON
Containers log to the console; let the platform collect. Don't write logs to files inside containers.

### 19. Set resource limits
Constrain CPU/memory so one service can't starve the host.

### 20. Make deploys reproducible and reversible
Pin versions, build deterministically, tag images, and keep the previous version available for
rollback. Document the deploy in a runbook.

### 21. Back up stateful volumes
Schedule database/object-storage backups and verify restores.

### 22. Keep the runbook current
A `deploy.md` should let someone provision from zero: prerequisites, env setup, DNS, certs,
`docker compose up`, smoke check, and rollback.

## Recommended Compose Shape

```txt
services:
  db         # postgres:17, healthcheck pg_isready, named volume
  minio      # object storage, named volume
  migrator   # one-shot: dotnet ef database update, depends_on db (healthy)
  api        # depends_on migrator (completed) + db (healthy)
  nginx      # publishes 80/443, serves SPA, proxies /api + docs, TLS
```

## Senior Rule

Build small reproducible images, keep secrets and internal services off the public surface, gate
startup on real readiness, migrate via a dedicated step, and make every deploy observable and
reversible.
