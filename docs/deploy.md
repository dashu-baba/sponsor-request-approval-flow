# Deployment Runbook

This is the initial Docker Compose walking skeleton. Production TLS, hardening, backups, and real
migrations are added in later tasks.

## Prerequisites

- Docker Engine with the Compose plugin.
- Ports `80` available on the host.
- For local development, no external PostgreSQL or MinIO installation is required.

## Environment Setup

The compose file defaults to `.env.example` so the skeleton can start with:

```bash
docker compose up --build
```

For local overrides, copy the example values and point Compose at the local file:

```bash
cp .env.example .env
docker compose --env-file .env up --build
```

Keep `.env` out of git. Replace every `change-me-*` value before using this outside local
development. The `COMPOSE_ENV_FILE` value in `.env.example` tells Compose which service env file to
mount; the default compose command still falls back to `.env.example` when no local `.env` exists.

## API routing (dev and Docker)

Browser API calls use the `/api` prefix (for example `/api/requests`, `/api/auth/login`). Both
local Vite dev and nginx in Docker strip that prefix before forwarding to the ASP.NET Core service,
which continues to expose routes at `/requests`, `/auth`, and so on. SPA page URLs such as
`/requests/7` are not prefixed and are served by the frontend router.

## Services

- `nginx` publishes host port `80`, serves the built SPA, and proxies `/api`, `/openapi`,
  and `/scalar` to the API container.
- `api` runs the ASP.NET Core service on the internal network.
- `db` runs PostgreSQL 17 with a named volume.
- `minio` runs S3-compatible object storage with a named volume.
- `migrator` is a one-shot placeholder. T1.1 replaces it with EF Core migration execution.

The `app-internal` network is marked `internal: true`, so services can talk to each other but do not
have outbound internet access from that network. Keep that restriction unless a later task adds a
runtime dependency that legitimately needs egress; in that case, attach only that service to a
separate external network.

## Smoke Checks

Start the stack:

```bash
docker compose up --build
```

In another terminal:

```bash
curl --fail http://localhost/
curl --fail http://localhost/api/health/ready
curl --fail http://localhost/api/health/live
```

`/health` and `/health/ready` verify dependencies (PostgreSQL, MinIO); `/health/live` checks the API process only. The health checks should return success once `db`, `minio`, `migrator`, `api`, and `nginx` are healthy.

### API auth smoke (through proxy)

After the stack is up, verify login, list, detail, and refresh through the `/api` prefix (same
path the SPA uses; Vite dev mirrors this behaviour):

```bash
COOKIE_JAR=$(mktemp)
curl --fail http://localhost/api/health

curl -s -c "$COOKIE_JAR" -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"requestor@demo.local","password":"Password1!"}' \
  | tee /tmp/login.json >/dev/null

TOKEN=$(python3 -c "import json; print(json.load(open('/tmp/login.json'))['accessToken'])")

curl --fail http://localhost/api/requests -H "Authorization: Bearer $TOKEN" >/dev/null
curl --fail http://localhost/api/requests/1 -H "Authorization: Bearer $TOKEN" >/dev/null
curl --fail -b "$COOKIE_JAR" -X POST http://localhost/api/auth/refresh >/dev/null

rm -f "$COOKIE_JAR" /tmp/login.json
```

Confirm the refresh cookie is scoped to `Path=/api/auth` (inspect `Set-Cookie` on login).
SPA routes such as `/requests/1` should return HTML, not API 401 responses.

## Shutdown

Stop containers while keeping data volumes:

```bash
docker compose down
```

Remove local data volumes when you want a clean reset:

```bash
docker compose down --volumes
```
